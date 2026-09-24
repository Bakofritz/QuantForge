using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Data;

public enum NinjaTraderExportResolution { Tick, Minute, Day, Unknown }

public sealed record MarketTick(DateTimeOffset Timestamp, decimal Price, long Volume);

public sealed record ImportedNinjaTraderExport(
    string SourcePath,
    NinjaTraderExportResolution Resolution,
    string DataType,
    IReadOnlyList<MarketTick> Ticks,
    IReadOnlyList<MarketBar> Bars,
    string ContentFingerprint);

public sealed class NinjaTraderExportImporter
{
    public ImportedNinjaTraderExport Import(string text, string sourcePath, NinjaTraderExportResolution resolution, string dataType = "Last")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var ticks = new List<MarketTick>();
        var bars = new List<MarketBar>();
        DateTimeOffset? previousTimestamp = null;
        var seenTimestamps = new HashSet<DateTimeOffset>();
        using var reader = new StringReader(text);
        string? line;
        var lineNo = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Date", StringComparison.OrdinalIgnoreCase)) continue;
            var f = line.Split(';');
            if (!DateTime.TryParseExact(f[0].Trim(), new[] { "yyyyMMdd HHmmss", "yyyyMMdd HHmm", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                throw new FormatException($"Invalid NinjaTrader export timestamp at line {lineNo}.");
            var ts = new DateTimeOffset(dt);
            if (previousTimestamp.HasValue && ts <= previousTimestamp.Value)
                throw new FormatException($"Non-chronological or duplicate timestamp at line {lineNo}: {ts:O}.");
            if (!seenTimestamps.Add(ts))
                throw new FormatException($"Duplicate timestamp at line {lineNo}: {ts:O}.");
            previousTimestamp = ts;
            if (resolution == NinjaTraderExportResolution.Tick)
            {
                if (f.Length < 3) throw new FormatException($"Tick export line {lineNo} requires timestamp;price;volume.");
                ticks.Add(new MarketTick(ts, ParseDecimal(f[1], lineNo), ParseLong(f[2], lineNo)));
            }
            else
            {
                if (f.Length < 6) throw new FormatException($"Bar export line {lineNo} requires timestamp;open;high;low;close;volume.");
                var o = ParseDecimal(f[1], lineNo); var h = ParseDecimal(f[2], lineNo); var l = ParseDecimal(f[3], lineNo); var c = ParseDecimal(f[4], lineNo); var v = ParseLong(f[5], lineNo);
                if (h < Math.Max(o, c) || l > Math.Min(o, c) || l > h) throw new FormatException($"Invalid OHLC relationship at line {lineNo}.");
                bars.Add(new MarketBar(ts, o, h, l, c, v));
            }
        }
        var canonical = resolution == NinjaTraderExportResolution.Tick
            ? string.Join("\n", ticks.Select(x => $"{x.Timestamp:O}|{x.Price.ToString(CultureInfo.InvariantCulture)}|{x.Volume}"))
            : string.Join("\n", bars.Select(x => $"{x.Timestamp:O}|{x.Open.ToString(CultureInfo.InvariantCulture)}|{x.High.ToString(CultureInfo.InvariantCulture)}|{x.Low.ToString(CultureInfo.InvariantCulture)}|{x.Close.ToString(CultureInfo.InvariantCulture)}|{x.Volume}"));
        return new ImportedNinjaTraderExport(sourcePath, resolution, dataType, ticks, bars, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant());
    }

    private static decimal ParseDecimal(string value, int line) => decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : throw new FormatException($"Invalid decimal at line {line}.");
    private static long ParseLong(string value, int line) => long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : throw new FormatException($"Invalid volume at line {line}.");
}

public sealed class TickToMinuteAggregator
{
    public IReadOnlyList<MarketBar> Aggregate(IEnumerable<MarketTick> ticks)
    {
        var ordered = ticks.OrderBy(x => x.Timestamp).ToArray();
        return ordered.GroupBy(x => new DateTimeOffset(x.Timestamp.Year, x.Timestamp.Month, x.Timestamp.Day, x.Timestamp.Hour, x.Timestamp.Minute, 0, TimeSpan.Zero))
            .OrderBy(g => g.Key)
            .Select(g => new MarketBar(g.Key, g.First().Price, g.Max(x => x.Price), g.Min(x => x.Price), g.Last().Price, g.Sum(x => x.Volume)))
            .ToArray();
    }
}
