using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Data;

public enum ValidationLayer { TickDirect, MinuteDerived, DayDerived, Coverage, Integrity }

public sealed record ValidationLayerResult(
    ValidationLayer Layer,
    bool Passed,
    string Description,
    int ComparedRecords,
    int Mismatches,
    IReadOnlyList<string> Warnings,
    string Fingerprint);

public sealed record TriangulatedNinjaTraderValidationResult(
    bool Passed,
    IReadOnlyList<ValidationLayerResult> Layers,
    string DatasetFingerprint,
    string ValidationFingerprint,
    string FidelityBoundary);

/// <summary>
/// Independent validation layer. It never promotes source data to trusted status by itself;
/// it only records agreement between separately acquired representations.
/// </summary>
public sealed class NinjaTraderValidationEngine
{
    public TriangulatedNinjaTraderValidationResult Validate(
        IReadOnlyList<MarketTick> nativeTicks,
        IReadOnlyList<MarketTick> exportedTicks,
        IReadOnlyList<MarketBar> nativeOrDerivedMinutes,
        IReadOnlyList<MarketBar> exportedMinutes,
        IReadOnlyList<MarketBar> nativeOrDerivedDays,
        IReadOnlyList<MarketBar> exportedDays,
        decimal tickSize,
        decimal priceTolerance = 0m)
    {
        ArgumentNullException.ThrowIfNull(nativeTicks);
        ArgumentNullException.ThrowIfNull(exportedTicks);
        ArgumentNullException.ThrowIfNull(nativeOrDerivedMinutes);
        ArgumentNullException.ThrowIfNull(exportedMinutes);
        ArgumentNullException.ThrowIfNull(nativeOrDerivedDays);
        ArgumentNullException.ThrowIfNull(exportedDays);
        if (tickSize <= 0) throw new ArgumentOutOfRangeException(nameof(tickSize));

        var layers = new List<ValidationLayerResult>
        {
            CompareTicks(nativeTicks, exportedTicks, tickSize, priceTolerance),
            CompareBars(ValidationLayer.MinuteDerived, nativeOrDerivedMinutes, exportedMinutes, priceTolerance),
            CompareBars(ValidationLayer.DayDerived, nativeOrDerivedDays, exportedDays, priceTolerance),
            CompareCoverage(nativeTicks, exportedTicks, nativeOrDerivedMinutes, exportedMinutes, nativeOrDerivedDays, exportedDays),
            ValidateIntegrity(nativeTicks, exportedTicks, nativeOrDerivedMinutes, exportedMinutes, nativeOrDerivedDays, exportedDays, tickSize)
        };

        var material = string.Join("|", layers.Select(x => $"{x.Layer}:{x.Passed}:{x.ComparedRecords}:{x.Mismatches}:{x.Fingerprint}"));
        var validationFingerprint = Hash(material);
        var datasetFingerprint = Hash(string.Join("\n", nativeTicks.Select(x => $"{x.Timestamp:O}|{x.Price.ToString(CultureInfo.InvariantCulture)}|{x.Volume}")));
        return new TriangulatedNinjaTraderValidationResult(
            layers.All(x => x.Passed), layers, datasetFingerprint, validationFingerprint,
            "Agreement validates only the supplied sample/range and declared fields. It does not prove untested native files, unavailable Level II, provider-independent correctness, or exchange execution behavior.");
    }

    private static ValidationLayerResult CompareTicks(IReadOnlyList<MarketTick> a, IReadOnlyList<MarketTick> b, decimal tickSize, decimal tolerance)
    {
        var map = b.GroupBy(x => x.Timestamp).ToDictionary(g => g.Key, g => g.ToArray());
        var mismatches = 0; var compared = 0; var warnings = new List<string>();
        foreach (var x in a)
        {
            if (!map.TryGetValue(x.Timestamp, out var candidates)) { mismatches++; continue; }
            var y = candidates.FirstOrDefault();
            compared++;
            if (y is null || Math.Abs(x.Price - y.Price) > tolerance || x.Volume != y.Volume) mismatches++;
            var nearest = decimal.Round(x.Price / tickSize, 0, MidpointRounding.AwayFromZero) * tickSize;
            if (Math.Abs(x.Price - nearest) > tolerance) warnings.Add("TICK_SIZE_VIOLATION");
        }
        if (a.Count != b.Count) warnings.Add("EVENT_COUNT_DIFFERENCE");
        return Layer(ValidationLayer.TickDirect, mismatches == 0 && a.Count == b.Count && !warnings.Contains("TICK_SIZE_VIOLATION"),
            "Direct native-vs-export tick comparison", compared, mismatches, warnings);
    }

    private static ValidationLayerResult CompareBars(ValidationLayer layer, IReadOnlyList<MarketBar> a, IReadOnlyList<MarketBar> b, decimal tolerance)
    {
        var map = b.GroupBy(x => x.Timestamp).ToDictionary(g => g.Key, g => g.First());
        var mismatches = 0; var compared = 0; var warnings = new List<string>();
        foreach (var x in a)
        {
            if (!map.TryGetValue(x.Timestamp, out var y)) { mismatches++; continue; }
            compared++;
            if (Math.Abs(x.Open-y.Open)>tolerance || Math.Abs(x.High-y.High)>tolerance || Math.Abs(x.Low-y.Low)>tolerance || Math.Abs(x.Close-y.Close)>tolerance || x.Volume != y.Volume) mismatches++;
        }
        if (a.Count != b.Count) warnings.Add("BAR_COUNT_DIFFERENCE");
        return Layer(layer, mismatches == 0 && a.Count == b.Count, $"{layer} native/derived-vs-export comparison", compared, mismatches, warnings);
    }

    private static ValidationLayerResult CompareCoverage(IReadOnlyList<MarketTick> nt, IReadOnlyList<MarketTick> et, IReadOnlyList<MarketBar> nm, IReadOnlyList<MarketBar> em, IReadOnlyList<MarketBar> nd, IReadOnlyList<MarketBar> ed)
    {
        var warnings = new List<string>();
        var checks = new[] { (nt.Count, et.Count, "TICK"), (nm.Count, em.Count, "MINUTE"), (nd.Count, ed.Count, "DAY") };
        foreach (var c in checks) if (c.Item1 != c.Item2) warnings.Add($"{c.Item3}_COVERAGE_DIFFERENCE");
        return Layer(ValidationLayer.Coverage, warnings.Count == 0, "Record-count coverage comparison", checks.Length, warnings.Count, warnings);
    }

    private static ValidationLayerResult ValidateIntegrity(IReadOnlyList<MarketTick> nt, IReadOnlyList<MarketTick> et, IReadOnlyList<MarketBar> nm, IReadOnlyList<MarketBar> em, IReadOnlyList<MarketBar> nd, IReadOnlyList<MarketBar> ed, decimal tickSize)
    {
        var warnings = new List<string>();
        ValidateTicks(nt, tickSize, warnings, "NATIVE_TICK");
        ValidateTicks(et, tickSize, warnings, "EXPORT_TICK");
        ValidateBars(nm, warnings, "NATIVE_MINUTE");
        ValidateBars(em, warnings, "EXPORT_MINUTE");
        ValidateBars(nd, warnings, "NATIVE_DAY");
        ValidateBars(ed, warnings, "EXPORT_DAY");
        return Layer(ValidationLayer.Integrity, warnings.Count == 0, "Chronology, uniqueness, OHLC, volume and tick-size integrity", 6, warnings.Count, warnings);
    }

    private static void ValidateTicks(IReadOnlyList<MarketTick> ticks, decimal tickSize, List<string> warnings, string label)
    {
        DateTimeOffset? previous = null;
        var seen = new HashSet<DateTimeOffset>();
        foreach (var t in ticks)
        {
            if (previous.HasValue && t.Timestamp <= previous.Value) warnings.Add($"{label}_NON_CHRONOLOGICAL");
            if (!seen.Add(t.Timestamp)) warnings.Add($"{label}_DUPLICATE_TIMESTAMP");
            if (t.Volume < 0) warnings.Add($"{label}_NEGATIVE_VOLUME");
            var nearest = decimal.Round(t.Price / tickSize, 0, MidpointRounding.AwayFromZero) * tickSize;
            if (Math.Abs(t.Price - nearest) > 0) warnings.Add($"{label}_TICK_SIZE_VIOLATION");
            previous = t.Timestamp;
        }
    }

    private static void ValidateBars(IReadOnlyList<MarketBar> bars, List<string> warnings, string label)
    {
        DateTimeOffset? previous = null;
        var seen = new HashSet<DateTimeOffset>();
        foreach (var b in bars)
        {
            if (previous.HasValue && b.Timestamp <= previous.Value) warnings.Add($"{label}_NON_CHRONOLOGICAL");
            if (!seen.Add(b.Timestamp)) warnings.Add($"{label}_DUPLICATE_TIMESTAMP");
            if (b.High < Math.Max(b.Open, b.Close) || b.Low > Math.Min(b.Open, b.Close) || b.Low > b.High) warnings.Add($"{label}_INVALID_OHLC");
            if (b.Volume < 0) warnings.Add($"{label}_NEGATIVE_VOLUME");
            previous = b.Timestamp;
        }
    }

    private static ValidationLayerResult Layer(ValidationLayer layer, bool passed, string description, int compared, int mismatches, IReadOnlyList<string> warnings)
        => new(layer, passed, description, compared, mismatches, warnings.Distinct().ToArray(), Hash($"{layer}|{passed}|{compared}|{mismatches}|{string.Join(',', warnings.OrderBy(x=>x))}"));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
