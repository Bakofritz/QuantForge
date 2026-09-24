using QuantForge.Core;
using QuantForge.Data;

namespace QuantForge.Backtesting;

public sealed record PositionLifecycleEvent(
    DateTimeOffset Timestamp,
    int PositionBefore,
    int PositionAfter,
    PositionIntentKind Intent,
    string Action,
    decimal ReferencePrice,
    decimal? FillPrice,
    string Reason,
    string Fingerprint);

public sealed record PositionLifecycleResult(
    AccountLedgerSnapshot FinalLedger,
    IReadOnlyList<LedgerTrade> Trades,
    IReadOnlyList<PositionLifecycleEvent> Events,
    IReadOnlyList<LedgerAccountingEvent> LedgerEvents,
    string EngineFingerprint,
    string EvidenceFingerprint,
    string FidelityDeclaration);

/// <summary>
/// Deterministic research-only position lifecycle. It converts canonical position intent
/// into close/open ledger transitions without broker or order-routing authority.
/// </summary>
public sealed class CanonicalPositionLifecycleRunner
{
    private readonly CausalTimeframeAggregator _aggregator = new();

    public async Task<PositionLifecycleResult> RunAsync(
        IReadOnlyList<MarketBar> bars,
        IStrategy strategy,
        TradingRiskSettings settings,
        IReadOnlyList<TimeSpan> timeframes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bars);
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(settings);
        if (bars.Count == 0) throw new ArgumentException("At least one market bar is required.", nameof(bars));
        if (timeframes is null || timeframes.Count == 0) throw new ArgumentException("At least one causal timeframe is required.", nameof(timeframes));

        var profile = settings.PrimaryInstrument switch
        {
            "MES" => QuantForgeInstrumentProfiles.MES,
            "MNQ" => QuantForgeInstrumentProfiles.MNQ with { EnabledForInitialResearch = true, Status = "RESEARCH" },
            _ => throw new InvalidOperationException("No governed instrument profile exists for the requested symbol.")
        };
        if (profile.TickSize != settings.TickSize || profile.TickValue != settings.TickValue)
            throw new InvalidOperationException("Risk settings do not match the governed instrument tick profile.");

        var ledger = new AccountLedger(settings, profile.PointValue);
        var trades = new List<LedgerTrade>();
        var events = new List<PositionLifecycleEvent>();
        DateTimeOffset? previous = null;
        var sequence = 0;
        var extremeSinceEntry = 0m;
        var entryPrice = 0m;
        var position = 0;

        foreach (var bar in bars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (previous is not null && bar.Timestamp <= previous.Value)
                throw new InvalidOperationException("Market bars must be strictly causal and chronological.");
            previous = bar.Timestamp;

            var eventTime = bar.Timestamp;
            var marketEvent = new MarketEvent(eventTime, settings.PrimaryInstrument, MarketEventKind.BarClose, bar.Close, bar.Volume);
            var tfViews = _aggregator.Snapshot(bars, eventTime, timeframes);
            var values = new Dictionary<string, decimal>(StringComparer.Ordinal)
            { ["PRICE"] = bar.Close, ["VOLUME"] = bar.Volume };
            var context = new StrategyContext(eventTime, values, sequence++, tfViews);
            var intent = strategy is ISideAwareStrategy sideAware
                ? sideAware.ObservePosition(context, marketEvent, position)
                : StrategyObservationAdapter.ToPositionIntent(strategy.Observe(context, marketEvent));

            // Protective exits always execute before a strategy reversal/entry request.
            if (position != 0)
            {
                var decision = SideAwareIntrabarResolver.Resolve(
                    bar, position, entryPrice, settings,
                    OhlcvIntrabarAmbiguityPolicy.ConservativeStopFirst,
                    extremeSinceEntry);
                if (decision.ExitTriggered)
                {
                    ClosePosition(ledger, trades, events, eventTime, bar, position, entryPrice, decision.Reason, settings, ref extremeSinceEntry, ref entryPrice);
                    position = 0;
                }
            }

            if (position != 0 && intent.Kind == PositionIntentKind.Exit)
            {
                ClosePosition(ledger, trades, events, eventTime, bar, position, entryPrice,
                    string.IsNullOrWhiteSpace(intent.Reason) ? "STRATEGY_EXIT" : intent.Reason,
                    settings, ref extremeSinceEntry, ref entryPrice);
                position = 0;
            }

            if (position != 0 && (intent.Kind == PositionIntentKind.ReverseToLong || intent.Kind == PositionIntentKind.ReverseToShort ||
                                  (intent.Kind == PositionIntentKind.EnterLong && position < 0) ||
                                  (intent.Kind == PositionIntentKind.EnterShort && position > 0)))
            {
                var target = intent.SignedQuantity;
                if (target != 0 && Math.Sign(target) != Math.Sign(position))
                {
                    ClosePosition(ledger, trades, events, eventTime, bar, position, entryPrice,
                        string.IsNullOrWhiteSpace(intent.Reason) ? "REVERSAL_EXIT" : $"{intent.Reason}_EXIT",
                        settings, ref extremeSinceEntry, ref entryPrice);
                    position = 0;
                }
            }

            if (position == 0 && intent.SignedQuantity != 0 &&
                (intent.Kind == PositionIntentKind.EnterLong || intent.Kind == PositionIntentKind.EnterShort ||
                 intent.Kind == PositionIntentKind.ReverseToLong || intent.Kind == PositionIntentKind.ReverseToShort))
            {
                var quantity = Math.Sign(intent.SignedQuantity) * Math.Min(Math.Abs(intent.SignedQuantity), settings.MaxContracts);
                var reference = bar.Close;
                var fill = SideAwareFillModel.EntryFill(reference, quantity, settings);
                if (ledger.CanOpen(eventTime, quantity, fill))
                {
                    ledger.Open(eventTime, quantity, fill);
                    var before = position;
                    position = quantity;
                    entryPrice = fill;
                    extremeSinceEntry = fill;
                    AddEvent(events, eventTime, before, position, intent.Kind, "ENTRY", reference, fill, intent.Reason);
                }
            }

            if (position != 0)
            {
                extremeSinceEntry = position > 0
                    ? Math.Max(extremeSinceEntry, bar.High)
                    : Math.Min(extremeSinceEntry, bar.Low);
            }

            await Task.Yield();
        }

        if (position != 0)
        {
            var last = bars[^1];
            var fill = SideAwareFillModel.ExitFill(last.Close, position, settings);
            ClosePosition(ledger, trades, events, last.Timestamp, last, position, entryPrice,
                "END_OF_DATA", settings, ref extremeSinceEntry, ref entryPrice, fill);
            position = 0;
        }

        var final = ledger.Snapshot(bars[^1].Close);
        var engineFingerprint = ResearchFingerprint.Sha256(
            $"POSITION_LIFECYCLE_V20.25|{settings.PrimaryInstrument}|{settings.InitialAccountSize}|{settings.MaxContracts}|{settings.InitialStopPoints}|{settings.TrailingActivationPoints}|{settings.TrailingDistancePoints}|{settings.ProfitTargetPoints}|{settings.CommissionPerSide}|{settings.SlippageTicksPerSide}|{OhlcvIntrabarAmbiguityPolicy.ConservativeStopFirst}");
        var evidenceFingerprint = ResearchFingerprint.Sha256(
            $"{engineFingerprint}|{final.Fingerprint}|{string.Join('|', trades.Select(t => t.Fingerprint))}|{string.Join('|', events.Select(e => e.Fingerprint))}|{string.Join('|', ledger.Events.Select(e => e.Fingerprint))}");

        return new(final, trades, events, ledger.Events, engineFingerprint, evidenceFingerprint,
            "Position lifecycle is deterministic research simulation. OHLCV does not establish intrabar ordering; barrier precedence is explicitly ConservativeStopFirst. Reversal is modeled as close-then-open and is never broker-routed.");
    }

    private static void ClosePosition(
        AccountLedger ledger,
        List<LedgerTrade> trades,
        List<PositionLifecycleEvent> events,
        DateTimeOffset time,
        MarketBar bar,
        int position,
        decimal entryPrice,
        string reason,
        TradingRiskSettings settings,
        ref decimal extremeSinceEntry,
        ref decimal storedEntryPrice,
        decimal? explicitFill = null)
    {
        var reference = explicitFill.HasValue ?
            (position > 0 ? explicitFill.Value + settings.SlippageTicksPerSide * settings.TickSize : explicitFill.Value - settings.SlippageTicksPerSide * settings.TickSize) : bar.Close;
        var fill = explicitFill ?? SideAwareFillModel.ExitFill(reference, position, settings);
        var commission = Math.Abs(position) * settings.CommissionPerSide;
        var slippageCost = Math.Abs(position) * settings.SlippageTicksPerSide * settings.TickValue;
        var before = position;
        var trade = ledger.Close(time, fill, reason, commission, slippageCost);
        trades.Add(trade);
        events.Add(new PositionLifecycleEvent(time, before, 0, PositionIntentKind.Exit, "EXIT", reference, fill, reason,
            ResearchFingerprint.Sha256($"EXIT|{time:O}|{before}|0|{reference}|{fill}|{reason}|{trade.Fingerprint}")));
        extremeSinceEntry = 0m;
        storedEntryPrice = 0m;
    }

    private static void AddEvent(List<PositionLifecycleEvent> events, DateTimeOffset time, int before, int after,
        PositionIntentKind intent, string action, decimal reference, decimal fill, string reason) =>
        events.Add(new PositionLifecycleEvent(time, before, after, intent, action, reference, fill, reason,
            ResearchFingerprint.Sha256($"{time:O}|{before}|{after}|{intent}|{action}|{reference}|{fill}|{reason}")));
}
