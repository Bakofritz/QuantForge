using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Data;

public sealed class NinjaTraderDataBridge
{
    public NinjaTraderSourceInventory Inventory(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException(rootPath);

        var files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Select(path => Describe(rootPath, path))
            .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inventoryText = string.Join("\n", files.Select(f => $"{f.RelativePath}|{f.LengthBytes}|{f.Sha256}"));
        var fingerprint = Hash(inventoryText);
        return new NinjaTraderSourceInventory(rootPath, files, fingerprint);
    }

    public NativeValidationPlan BuildValidationPlan(string instrument, DateTimeOffset firstDate, DateTimeOffset lastDate)
    {
        if (lastDate < firstDate) throw new ArgumentException("lastDate must be >= firstDate");
        var span = lastDate - firstDate;
        var days = Math.Max(1, (int)Math.Ceiling(span.TotalDays));
        var samples = new List<DataCrossValidationSample>();
        // Small, deliberate samples are intended to validate parser fidelity rather than reproduce the full archive.
        var candidateOffsets = new[] { 0, days / 2, Math.Max(0, days - 1) }.Distinct();
        foreach (var offset in candidateOffsets)
        {
            var start = firstDate.AddDays(offset);
            var end = start.AddDays(1);
            samples.Add(new DataCrossValidationSample(start, end, instrument, "Tick", "NT8 Native", "NT8 Tick Export TXT"));
        }
        samples.Add(new DataCrossValidationSample(firstDate, lastDate, instrument, "Minute", "NT8 Native/Derived", "NT8 Minute Export TXT"));
        samples.Add(new DataCrossValidationSample(firstDate, lastDate, instrument, "Day", "NT8 Native/Derived", "NT8 Day Export TXT"));
        return new NativeValidationPlan(
            samples,
            "Use a few complete representative tick sessions to validate native extraction and use long minute/day exports to validate continuity, aggregation, sessions, and contract/date coverage without requiring a multi-GB tick archive.",
            "Agreement with exports validates the compared sample and declared fields only; it does not prove unavailable Level II data, provider-independent accuracy, or every untested native file.");
    }

    private static NinjaTraderSourceFile Describe(string root, string path)
    {
        var info = new FileInfo(path);
        var relative = Path.GetRelativePath(root, path);
        var name = info.Name.ToLowerInvariant();
        var kind = name.Contains("replay") || info.Extension.Equals(".nrt", StringComparison.OrdinalIgnoreCase)
            ? NinjaTraderSourceKind.NativeReplay
            : name.Contains("historical") || info.DirectoryName?.Contains("historical", StringComparison.OrdinalIgnoreCase) == true
                ? NinjaTraderSourceKind.NativeHistorical
                : NinjaTraderSourceKind.Unknown;
        return new NinjaTraderSourceFile(path, relative, kind, info.Length, FileSha256(path), info.LastWriteTimeUtc);
    }

    private static string FileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class MarketDataCrossValidator
{
    public DataCrossValidationResult Compare(IReadOnlyList<MarketBar> a, IReadOnlyList<MarketBar> b, decimal tickSize, decimal priceTolerance = 0m)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (tickSize <= 0) throw new ArgumentOutOfRangeException(nameof(tickSize));
        var warnings = new List<string>();
        var map = b.ToDictionary(x => x.Timestamp);
        var compared = 0;
        var timestampMismatches = 0;
        var ohlcMismatches = 0;
        var volumeMismatches = 0;
        var tickViolations = 0;
        decimal maxPriceDiff = 0;
        long maxVolumeDiff = 0;

        foreach (var bar in a)
        {
            if (!map.TryGetValue(bar.Timestamp, out var other)) { timestampMismatches++; continue; }
            compared++;
            foreach (var p in new[] { bar.Open, bar.High, bar.Low, bar.Close })
            {
                var nearestTicks = decimal.Round(p / tickSize, 0, MidpointRounding.AwayFromZero) * tickSize;
                if (Math.Abs(p - nearestTicks) > priceTolerance) tickViolations++;
            }
            var diffs = new[] { Math.Abs(bar.Open - other.Open), Math.Abs(bar.High - other.High), Math.Abs(bar.Low - other.Low), Math.Abs(bar.Close - other.Close) };
            var localMax = diffs.Max();
            maxPriceDiff = Math.Max(maxPriceDiff, localMax);
            if (localMax > priceTolerance) ohlcMismatches++;
            var vdiff = Math.Abs(bar.Volume - other.Volume);
            maxVolumeDiff = Math.Max(maxVolumeDiff, vdiff);
            if (vdiff != 0) volumeMismatches++;
        }
        if (timestampMismatches > 0) warnings.Add("TIMESTAMP_COVERAGE_DIFFERENCE");
        if (ohlcMismatches > 0) warnings.Add("PRICE_DIFFERENCE_PRESENT");
        if (volumeMismatches > 0) warnings.Add("VOLUME_DIFFERENCE_PRESENT");
        var passed = timestampMismatches == 0 && ohlcMismatches == 0 && tickViolations == 0;
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{compared}|{timestampMismatches}|{ohlcMismatches}|{volumeMismatches}|{maxPriceDiff.ToString(CultureInfo.InvariantCulture)}|{maxVolumeDiff}"))).ToLowerInvariant();
        return new DataCrossValidationResult(passed, compared, timestampMismatches, ohlcMismatches, volumeMismatches, tickViolations, maxPriceDiff, maxVolumeDiff, warnings, fingerprint);
    }
}
