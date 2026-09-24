using QuantForge.Core;
using QuantForge.Data;

namespace QuantForge.Backtesting;

public enum CanonicalIndicatorKind { Ema, Sma, Atr }
public enum CanonicalSignalKind { CrossUp, CrossDown }

public sealed record CanonicalIndicatorDefinition(
    CanonicalIndicatorKind Kind,
    string Name,
    int Period,
    string Source,
    bool PeriodIsSourceDerived);

public sealed record CanonicalSignalDefinition(
    CanonicalSignalKind Kind,
    string Name,
    string Source,
    bool SourceDerived);

public sealed record CanonicalChartMarker(
    DateTimeOffset Timestamp,
    CanonicalSignalKind Signal,
    decimal Price,
    string Label);

public sealed record CanonicalStrategyResearchPlan(
    string StrategyId,
    string ModelFingerprint,
    IReadOnlyList<CanonicalIndicatorDefinition> Indicators,
    IReadOnlyList<CanonicalSignalDefinition> Signals,
    IReadOnlyList<string> UnsupportedFeatures,
    bool BacktestSupported,
    string FidelityDeclaration,
    string PlanFingerprint);

public sealed record CanonicalIndicatorSeries(
    string IndicatorName,
    IReadOnlyList<decimal?> Values,
    string Fingerprint);

public sealed record CanonicalSignalSeries(
    IReadOnlyList<CanonicalChartMarker> Markers,
    string Fingerprint);

/// <summary>
/// Converts only explicitly recognized canonical semantics into deterministic research artifacts.
/// It never interprets raw source code and never grants execution authority.
/// </summary>
public sealed partial class CanonicalStrategyAdapter
{
    public CanonicalStrategyResearchPlan CreateResearchPlan(CanonicalStrategyModel model, int defaultFastPeriod = 9, int defaultSlowPeriod = 21)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (!model.ResearchEligible) throw new InvalidOperationException("Canonical model is not research eligible.");
        if (defaultFastPeriod <= 0 || defaultSlowPeriod <= defaultFastPeriod) throw new ArgumentOutOfRangeException(nameof(defaultSlowPeriod));

        var indicators = new List<CanonicalIndicatorDefinition>();
        var signals = new List<CanonicalSignalDefinition>();
        var unsupported = new List<string>();

        var hasEma = model.Indicators.Any(x => x.Contains("EMA", StringComparison.OrdinalIgnoreCase));
        var hasSma = model.Indicators.Any(x => x.Contains("SMA", StringComparison.OrdinalIgnoreCase));
        var hasCross = model.EntryRules.Any(x => x.Contains("Cross", StringComparison.OrdinalIgnoreCase));

        if (hasEma && hasCross)
        {
            indicators.Add(new CanonicalIndicatorDefinition(CanonicalIndicatorKind.Ema, "EMA fast", defaultFastPeriod, "adapter-default; source specifies EMA family but not period", false));
            indicators.Add(new CanonicalIndicatorDefinition(CanonicalIndicatorKind.Ema, "EMA slow", defaultSlowPeriod, "adapter-default; source specifies EMA family but not period", false));
            signals.Add(new CanonicalSignalDefinition(CanonicalSignalKind.CrossUp, "Cross up", "canonical Cross signal", true));
            signals.Add(new CanonicalSignalDefinition(CanonicalSignalKind.CrossDown, "Cross down", "canonical Cross signal", true));
        }
        else if (hasSma && hasCross)
        {
            indicators.Add(new CanonicalIndicatorDefinition(CanonicalIndicatorKind.Sma, "SMA fast", defaultFastPeriod, "adapter-default; source specifies SMA family but not period", false));
            indicators.Add(new CanonicalIndicatorDefinition(CanonicalIndicatorKind.Sma, "SMA slow", defaultSlowPeriod, "adapter-default; source specifies SMA family but not period", false));
            signals.Add(new CanonicalSignalDefinition(CanonicalSignalKind.CrossUp, "Cross up", "canonical Cross signal; indicator-only until SMA execution adapter is declared", true));
            signals.Add(new CanonicalSignalDefinition(CanonicalSignalKind.CrossDown, "Cross down", "canonical Cross signal; indicator-only until SMA execution adapter is declared", true));
            unsupported.Add("SMA crossover execution is not enabled by the current EMA-only execution engine; SMA indicator evaluation is available for research visualization.");
        }
        else
        {
            unsupported.Add("No supported canonical crossover semantics were found for the current deterministic engine.");
        }

        foreach (var rule in model.ExitRules.Concat(model.RiskRules).Concat(model.TimeframeRules))
        {
            if (!rule.Contains("Cross", StringComparison.OrdinalIgnoreCase) && !rule.Contains("Stop", StringComparison.OrdinalIgnoreCase) && !rule.Contains("Target", StringComparison.OrdinalIgnoreCase))
                unsupported.Add($"Unsupported canonical rule: {rule}");
        }

        var supported = indicators.Count > 0 && signals.Count >= 2 && unsupported.Count == 0;
        var fingerprint = Hash(string.Join("|", model.ModelFingerprint,
            string.Join(";", indicators.Select(i => $"{i.Kind}:{i.Name}:{i.Period}:{i.Source}:{i.PeriodIsSourceDerived}")),
            string.Join(";", signals.Select(s => $"{s.Kind}:{s.Name}:{s.Source}:{s.SourceDerived}")),
            string.Join(";", unsupported.Order(StringComparer.Ordinal))));

        return new CanonicalStrategyResearchPlan(model.StrategyId, model.ModelFingerprint, indicators, signals,
            unsupported.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), supported,
            "OHLCV bar-driven canonical research; adapter does not create tick, bid/ask, spread, replay, or intrabar sequencing fidelity.", fingerprint);
    }

    public CanonicalIndicatorSeries EvaluateEma(IReadOnlyList<MarketBar> bars, CanonicalIndicatorDefinition definition)
    {
        if (definition.Kind != CanonicalIndicatorKind.Ema) throw new ArgumentException("Indicator definition must be EMA.", nameof(definition));
        if (definition.Period <= 0) throw new ArgumentOutOfRangeException(nameof(definition));
        ValidateChronology(bars);
        var values = new decimal?[bars.Count];
        if (bars.Count == 0) return new(definition.Name, values, Hash(definition.Name));
        var alpha = 2m / (definition.Period + 1);
        var ema = bars[0].Close;
        values[0] = ema;
        for (var i = 1; i < bars.Count; i++) { ema = ((bars[i].Close - ema) * alpha) + ema; values[i] = ema; }
        return new(definition.Name, values, Hash(string.Join("|", definition.Name, definition.Period, string.Join(",", values))));
    }

    public CanonicalSignalSeries EvaluateEmaCross(IReadOnlyList<MarketBar> bars, CanonicalIndicatorSeries fast, CanonicalIndicatorSeries slow)
    {
        ValidateChronology(bars);
        if (fast.Values.Count != bars.Count || slow.Values.Count != bars.Count) throw new ArgumentException("Indicator series length must equal bar count.");
        var markers = new List<CanonicalChartMarker>();
        for (var i = 1; i < bars.Count; i++)
        {
            var pf = fast.Values[i - 1]; var ps = slow.Values[i - 1]; var f = fast.Values[i]; var s = slow.Values[i];
            if (pf is null || ps is null || f is null || s is null) continue;
            if (pf <= ps && f > s) markers.Add(new(bars[i].Timestamp, CanonicalSignalKind.CrossUp, bars[i].Close, "EMA Cross Up"));
            else if (pf >= ps && f < s) markers.Add(new(bars[i].Timestamp, CanonicalSignalKind.CrossDown, bars[i].Close, "EMA Cross Down"));
        }
        return new(markers, Hash(string.Join("|", markers.Select(m => $"{m.Timestamp:O}:{m.Signal}:{m.Price}"))));
    }

    private static void ValidateChronology(IReadOnlyList<MarketBar> bars)
    {
        for (var i = 1; i < bars.Count; i++) if (bars[i - 1].Timestamp >= bars[i].Timestamp) throw new ArgumentException("Bars must be strictly chronological with unique timestamps.", nameof(bars));
    }
    private static string Hash(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record CanonicalBacktestAdapterResult(
    DeterministicBacktestResult Result,
    CanonicalStrategyExecutionPlan ExecutionPlan,
    string AdapterEvidenceFingerprint);

public sealed record CanonicalIndicatorArtifactResult(
    CanonicalIndicatorArtifact Artifact,
    IReadOnlyList<CanonicalIndicatorSeries> Series,
    CanonicalSignalSeries Signals);

public partial class CanonicalStrategyAdapter
{
    public CanonicalBacktestAdapterResult RunBacktest(
        IReadOnlyList<QuantForge.Data.MarketBar> bars,
        QuantForge.Backtesting.BacktestRequest request,
        CanonicalStrategySemantics semantics,
        decimal pointValue = 1m,
        decimal commissionPoints = 0m,
        TradingRiskSettings? riskSettings = null)
    {
        ArgumentNullException.ThrowIfNull(semantics);
        var fast = semantics.Indicators.FirstOrDefault(x => x.Name.Contains("fast", StringComparison.OrdinalIgnoreCase));
        var slow = semantics.Indicators.FirstOrDefault(x => x.Name.Contains("slow", StringComparison.OrdinalIgnoreCase));
        var unsupported = semantics.UnsupportedSemantics.ToArray();
        var supported = fast is not null && slow is not null && unsupported.Length == 0;
        if (!supported) throw new InvalidOperationException("Canonical strategy semantics are not fully supported: " + string.Join(" | ", unsupported));

        var fastPeriod = (int)fast!.Parameters.Single(x => x.Name == "Period").DefaultValue;
        var slowPeriod = (int)slow!.Parameters.Single(x => x.Name == "Period").DefaultValue;
        var engine = new EmaCrossBacktestEngine();
        var result = engine.Run(bars, request, fastPeriod, slowPeriod, pointValue, commissionPoints);
        var risk = riskSettings ?? TradingRiskSettings.DefaultMes();
        var riskSnapshot = RiskManagementRules.Evaluate(risk);
        var plan = new CanonicalStrategyExecutionPlan(semantics.StrategyId, semantics.SemanticsFingerprint, QuantForge.Core.CanonicalDirection.Long,
            fastPeriod, slowPeriod, pointValue, commissionPoints, true, Array.Empty<string>(),
            Hash($"{semantics.SemanticsFingerprint}|{fastPeriod}|{slowPeriod}|{pointValue}|{commissionPoints}|{riskSnapshot.Fingerprint}"), result.Result.FidelityDeclaration, riskSnapshot.Fingerprint);
        return new CanonicalBacktestAdapterResult(result, plan, Hash($"{plan.ExecutionPlanFingerprint}|{result.Result.ResultHash}"));
    }

    public CanonicalIndicatorArtifactResult BuildIndicatorArtifact(
        IReadOnlyList<QuantForge.Data.MarketBar> bars,
        CanonicalStrategySemantics semantics)
    {
        ArgumentNullException.ThrowIfNull(semantics);
        var plan = CreateResearchPlan(new QuantForge.Core.CanonicalStrategyModel(
            semantics.StrategyId,
            "canonical",
            semantics.SemanticsFingerprint,
            semantics.Indicators.Select(x => x.Name).ToArray(),
            semantics.Entries.Select(x => x.Trigger).ToArray(),
            semantics.Exits.Select(x => x.Expression).ToArray(),
            semantics.Risks.Select(x => x.Expression).ToArray(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            semantics.SemanticsFingerprint,
            true,
            false));
        if (!plan.BacktestSupported) throw new InvalidOperationException("Canonical indicator artifact contains unsupported semantics.");
        var fast = EvaluateEma(bars, plan.Indicators.Single(x => x.Name == "EMA fast"));
        var slow = EvaluateEma(bars, plan.Indicators.Single(x => x.Name == "EMA slow"));
        var signals = EvaluateEmaCross(bars, fast, slow);
        var artifactId = "indicator-" + Hash($"{semantics.StrategyId}|{semantics.SemanticsFingerprint}")[..16];
        var artifact = new QuantForge.Core.CanonicalIndicatorArtifact(semantics.StrategyId, artifactId, semantics.SemanticsFingerprint,
            new[] { "ema-fast", "ema-slow" }, Hash($"{artifactId}|{fast.Fingerprint}|{slow.Fingerprint}|{signals.Fingerprint}"),
            "OHLCV bar-driven indicator; markers are research/visual artifacts and carry no order authority.");
        return new CanonicalIndicatorArtifactResult(artifact, new[] { fast, slow }, signals);
    }

}

public sealed record CanonicalTimeframeBinding(
    string EntryId,
    TimeSpan Timeframe,
    string TimeframeProvenance,
    string BindingFingerprint);

public sealed record CanonicalCausalObservation(
    DateTimeOffset EventTime,
    IReadOnlyDictionary<string, QuantForge.Core.CausalBarView> Timeframes,
    string ObservationFingerprint);

public partial class CanonicalStrategyAdapter
{
    public CanonicalIndicatorSeries EvaluateSma(IReadOnlyList<MarketBar> bars, CanonicalIndicatorDefinition definition)
    {
        if (definition.Kind != CanonicalIndicatorKind.Sma) throw new ArgumentException("Indicator definition must be SMA.", nameof(definition));
        if (definition.Period <= 0) throw new ArgumentOutOfRangeException(nameof(definition));
        ValidateChronology(bars);
        var values = new decimal?[bars.Count];
        decimal rolling = 0m;
        var queue = new Queue<decimal>();
        for (var i = 0; i < bars.Count; i++)
        {
            var value = bars[i].Close;
            queue.Enqueue(value);
            rolling += value;
            if (queue.Count > definition.Period) rolling -= queue.Dequeue();
            if (queue.Count == definition.Period) values[i] = rolling / definition.Period;
        }
        return new(definition.Name, values, Hash(string.Join("|", definition.Name, definition.Period, string.Join(",", values))));
    }

    public CanonicalIndicatorSeries EvaluateAtr(IReadOnlyList<MarketBar> bars, CanonicalIndicatorDefinition definition)
    {
        if (definition.Kind != CanonicalIndicatorKind.Atr) throw new ArgumentException("Indicator definition must be ATR.", nameof(definition));
        if (definition.Period <= 0) throw new ArgumentOutOfRangeException(nameof(definition));
        ValidateChronology(bars);
        var values = new decimal?[bars.Count];
        if (bars.Count == 0) return new(definition.Name, values, Hash(definition.Name));
        var trueRanges = new decimal[bars.Count];
        trueRanges[0] = bars[0].High - bars[0].Low;
        for (var i = 1; i < bars.Count; i++)
        {
            var previousClose = bars[i - 1].Close;
            trueRanges[i] = Math.Max(bars[i].High - bars[i].Low,
                Math.Max(Math.Abs(bars[i].High - previousClose), Math.Abs(bars[i].Low - previousClose)));
        }
        decimal rolling = 0m;
        for (var i = 0; i < bars.Count; i++)
        {
            rolling += trueRanges[i];
            if (i >= definition.Period) rolling -= trueRanges[i - definition.Period];
            if (i >= definition.Period - 1) values[i] = rolling / definition.Period;
        }
        return new(definition.Name, values, Hash(string.Join("|", definition.Name, definition.Period, string.Join(",", values))));
    }

    public IReadOnlyList<CanonicalTimeframeBinding> BindTimeframes(CanonicalStrategySemantics semantics, IReadOnlyDictionary<string, TimeSpan> available)
    {
        ArgumentNullException.ThrowIfNull(semantics);
        ArgumentNullException.ThrowIfNull(available);
        var bindings = new List<CanonicalTimeframeBinding>();
        foreach (var entry in semantics.Entries)
        {
            if (string.Equals(entry.Timeframe, "source-not-explicit", StringComparison.OrdinalIgnoreCase))
            {
                bindings.Add(new(entry.EntryId, TimeSpan.Zero, entry.Provenance, Hash($"{entry.EntryId}|unspecified")));
                continue;
            }
            if (!available.TryGetValue(entry.Timeframe, out var timeframe) || timeframe <= TimeSpan.Zero)
                throw new InvalidOperationException($"Canonical timeframe '{entry.Timeframe}' is unavailable for entry '{entry.EntryId}'.");
            bindings.Add(new(entry.EntryId, timeframe, entry.Provenance, Hash($"{entry.EntryId}|{entry.Timeframe}|{timeframe.Ticks}")));
        }
        return bindings;
    }

    public CanonicalCausalObservation ObserveCausally(IReadOnlyList<MarketBar> bars, DateTimeOffset eventTime, IReadOnlyList<TimeSpan> timeframes)
    {
        ValidateChronology(bars);
        if (timeframes is null || timeframes.Count == 0) throw new ArgumentException("At least one timeframe is required.", nameof(timeframes));
        var aggregator = new QuantForge.Data.CausalTimeframeAggregator();
        var snapshot = aggregator.Snapshot(bars, eventTime, timeframes);
        var fingerprint = Hash(string.Join("|", eventTime.ToUniversalTime().ToString("O"),
            snapshot.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x =>
                $"{x.Key}:{x.Value.Start:O}:{x.Value.End:O}:{x.Value.Open}:{x.Value.High}:{x.Value.Low}:{x.Value.Close}:{x.Value.Volume}:{x.Value.IsComplete}")));
        return new(eventTime, snapshot, fingerprint);
    }
}
