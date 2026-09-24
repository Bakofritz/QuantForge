using System.Security.Cryptography;
using System.Text;
using QuantForge.Core;

namespace QuantForge.Backtesting;

/// <summary>Builds structured canonical semantics without executing imported source.</summary>
public sealed class CanonicalStrategySemanticsAdapter
{
    public CanonicalStrategySemantics Build(CanonicalStrategyModel model, int defaultFastPeriod = 9, int defaultSlowPeriod = 21)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (!model.ResearchEligible) throw new InvalidOperationException("Canonical model is not research eligible.");
        if (defaultFastPeriod <= 0 || defaultSlowPeriod <= defaultFastPeriod) throw new ArgumentOutOfRangeException(nameof(defaultSlowPeriod));

        var indicators = new List<CanonicalIndicatorSemantics>();
        var entries = new List<CanonicalEntrySemantics>();
        var exits = new List<CanonicalExitSemantics>();
        var risks = new List<CanonicalRiskSemantics>();
        var unsupported = new List<string>();

        var hasEma = model.Indicators.Any(x => x.Contains("EMA", StringComparison.OrdinalIgnoreCase));
        var hasSma = model.Indicators.Any(x => x.Contains("SMA", StringComparison.OrdinalIgnoreCase));
        var hasCross = model.EntryRules.Any(x => x.Contains("Cross", StringComparison.OrdinalIgnoreCase));

        if (hasEma && hasCross)
        {
            indicators.Add(CreateIndicator("ema-fast", "EMA fast", defaultFastPeriod, model.SourceFingerprint));
            indicators.Add(CreateIndicator("ema-slow", "EMA slow", defaultSlowPeriod, model.SourceFingerprint));
            entries.Add(CreateEntry("long-cross-up", CanonicalDirection.Long, "EMA fast crosses above EMA slow", new[] { "ema-fast", "ema-slow" }, model.SourceFingerprint));
            entries.Add(CreateEntry("flat-cross-down", CanonicalDirection.Flat, "EMA fast crosses below EMA slow", new[] { "ema-fast", "ema-slow" }, model.SourceFingerprint));
        }
        else if (hasSma && hasCross)
        {
            unsupported.Add("SMA crossover semantics are recognized but the current deterministic execution adapter supports EMA crossover only.");
        }
        else
        {
            unsupported.Add("No supported canonical crossover semantics were found.");
        }

        foreach (var rule in model.ExitRules)
        {
            if (rule.Contains("Cross", StringComparison.OrdinalIgnoreCase))
                exits.Add(CreateExit("opposing-cross", CanonicalExitKind.OpposingSignal, CanonicalDirection.Long, rule, null, model.SourceFingerprint));
            else if (rule.Contains("Stop", StringComparison.OrdinalIgnoreCase))
                unsupported.Add($"Stop rule requires explicit numeric/semantic extraction: {rule}");
            else if (rule.Contains("Target", StringComparison.OrdinalIgnoreCase))
                unsupported.Add($"Target rule requires explicit numeric/semantic extraction: {rule}");
            else
                unsupported.Add($"Unsupported exit semantics: {rule}");
        }

        foreach (var rule in model.RiskRules)
            risks.Add(CreateRisk(rule, model.SourceFingerprint));

        var normalized = string.Join("|",
            model.StrategyId,
            model.ModelFingerprint,
            string.Join(";", indicators.Select(x => x.Fingerprint).Order(StringComparer.Ordinal)),
            string.Join(";", entries.Select(x => x.Fingerprint).Order(StringComparer.Ordinal)),
            string.Join(";", exits.Select(x => x.Fingerprint).Order(StringComparer.Ordinal)),
            string.Join(";", risks.Select(x => x.Fingerprint).Order(StringComparer.Ordinal)),
            string.Join(";", unsupported.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)));

        return new CanonicalStrategySemantics(model.StrategyId, model.ModelFingerprint, indicators, entries, exits, risks,
            unsupported.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), Hash(normalized));
    }

    private static CanonicalIndicatorSemantics CreateIndicator(string id, string name, int period, string sourceFingerprint)
    {
        var parameter = new CanonicalParameterDefinition($"{id}-period", "Period", period, 1, 1000, 1,
            "adapter-default; source identified EMA family but did not provide a numeric period", false, Hash($"{id}|period|{period}|{sourceFingerprint}"));
        return new CanonicalIndicatorSemantics(id, name, "EMA", CanonicalPriceSource.Close, new[] { parameter }, parameter.Provenance, Hash($"{id}|EMA|Close|{parameter.Fingerprint}"));
    }

    private static CanonicalEntrySemantics CreateEntry(string id, CanonicalDirection direction, string trigger, IReadOnlyList<string> indicators, string sourceFingerprint) =>
        new(id, direction, trigger, indicators, "source-not-explicit", "canonical-model; trigger semantics recognized; timeframe not explicitly supplied", Hash($"{id}|{direction}|{trigger}|{string.Join(',', indicators)}|{sourceFingerprint}"));

    private static CanonicalExitSemantics CreateExit(string id, CanonicalExitKind kind, CanonicalDirection direction, string expression, decimal? value, string sourceFingerprint) =>
        new(id, kind, direction, expression, value, "canonical-model; opposing cross semantics recognized", Hash($"{id}|{kind}|{direction}|{expression}|{sourceFingerprint}"));

    private static CanonicalRiskSemantics CreateRisk(string expression, string sourceFingerprint) =>
        new(Hash($"risk|{expression}|{sourceFingerprint}")[..16], expression, expression, null, "canonical-model; value not numerically extracted", Hash($"risk|{expression}|{sourceFingerprint}"));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
