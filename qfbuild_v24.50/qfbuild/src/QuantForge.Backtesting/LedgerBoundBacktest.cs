using QuantForge.Core;
using QuantForge.Data;

namespace QuantForge.Backtesting;

public sealed record LedgerBoundBacktestResult(
    string Instrument,
    decimal InitialEquity,
    AccountLedgerSnapshot FinalLedger,
    IReadOnlyList<LedgerTrade> Trades,
    string EngineFingerprint,
    string EvidenceFingerprint,
    string FidelityDeclaration);

/// <summary>
/// Research-only event runner that binds simulated fills to the account ledger.
/// It has no broker, order-routing, or live-trading authority.
/// </summary>
public sealed class LedgerBoundBacktestRunner
{
    private readonly CausalTimeframeAggregator _aggregator = new();

    public async Task<LedgerBoundBacktestResult> RunAsync(
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
        DateTimeOffset? previous = null;
        var sequence = 0;
        var open = false;
        var entryFill = 0m;
        var highestSinceEntry = 0m;

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
            var observation = strategy.Observe(context, marketEvent);

            if (open)
            {
                highestSinceEntry = Math.Max(highestSinceEntry, bar.High);
                var exitReason = ResolveExitReason(bar, entryFill, highestSinceEntry, settings);
                if (observation.ExitLong && exitReason == "NONE") exitReason = observation.Reason ?? "STRATEGY_EXIT";
                if (exitReason != "NONE")
                {
                    var exitPrice = ResolveExitPrice(bar, entryFill, highestSinceEntry, settings, exitReason);
                    var commission = settings.CommissionPerSide;
                    var slippageCost = settings.SlippageTicksPerSide * settings.TickValue;
                    var trade = ledger.Close(eventTime, exitPrice, exitReason, commission, slippageCost);
                    trades.Add(trade);
                    open = false;
                }
            }

            if (!open && observation.EnterLong && ledger.CanOpen(eventTime, 1, bar.Close))
            {
                entryFill = bar.Close + settings.SlippageTicksPerSide * settings.TickSize;
                highestSinceEntry = entryFill;
                ledger.Open(eventTime, 1, entryFill);
                open = true;
            }

            await Task.Yield();
        }

        if (open)
        {
            var last = bars[^1];
            var exitPrice = last.Close - settings.SlippageTicksPerSide * settings.TickSize;
            var trade = ledger.Close(last.Timestamp, exitPrice, "END_OF_DATA", settings.CommissionPerSide, settings.SlippageTicksPerSide * settings.TickValue);
            trades.Add(trade);
        }

        var final = ledger.Snapshot(bars[^1].Close);
        var engineFingerprint = ResearchFingerprint.Sha256($"LEDGER_BOUND_V20.22|{settings.PrimaryInstrument}|{settings.InitialAccountSize}|{settings.MaxContracts}|{settings.InitialStopPoints}|{settings.TrailingActivationPoints}|{settings.TrailingDistancePoints}|{settings.ProfitTargetPoints}|{settings.CommissionPerSide}|{settings.SlippageTicksPerSide}");
        var evidenceFingerprint = ResearchFingerprint.Sha256($"{engineFingerprint}|{final.Fingerprint}|{string.Join('|', trades.Select(t => t.Fingerprint))}");
        return new(settings.PrimaryInstrument, settings.InitialAccountSize, final, trades, engineFingerprint, evidenceFingerprint,
            "Ledger is a deterministic research accounting model. OHLCV cannot establish intrabar event ordering; stop/target/trailing behavior is policy-resolved and not observed tick execution.");
    }

    private static string ResolveExitReason(MarketBar bar, decimal entry, decimal highestSinceEntry, TradingRiskSettings settings)
    {
        var target = settings.ProfitTargetPoints > 0m ? entry + settings.ProfitTargetPoints : (decimal?)null;
        var stop = entry - settings.InitialStopPoints;
        var decision = OhlcvIntrabarResolver.ResolveLong(bar.Open, bar.High, bar.Low, bar.Close, entry, target, stop, OhlcvIntrabarAmbiguityPolicy.ConservativeStopFirst);
        if (decision.ExitTriggered) return decision.Reason;
        return highestSinceEntry >= entry + settings.TrailingActivationPoints && bar.Low <= highestSinceEntry - settings.TrailingDistancePoints ? "TRAILING_STOP" : "NONE";
    }

    private static decimal ResolveExitPrice(MarketBar bar, decimal entry, decimal highestSinceEntry, TradingRiskSettings settings, string reason)
    {
        return reason switch
        {
            "PROFIT_TARGET" => entry + settings.ProfitTargetPoints,
            "INITIAL_STOP" => entry - settings.InitialStopPoints,
            "TRAILING_STOP" => highestSinceEntry - settings.TrailingDistancePoints,
            _ => bar.Close
        };
    }
}
