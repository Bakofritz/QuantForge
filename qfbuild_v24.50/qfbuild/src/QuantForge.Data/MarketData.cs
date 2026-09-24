using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Data;

public sealed record MarketBar(
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);

public sealed record ImportedMarketDataset(
    string DatasetId,
    string Instrument,
    string Timeframe,
    IReadOnlyList<MarketBar> Bars,
    MarketDataFingerprint Fingerprint,
    DataFidelityProfile Fidelity);

public sealed class DelimitedOhlcvImporter
{
    public ImportedMarketDataset Import(
        string text,
        string instrument,
        string timeframe,
        string datasetId,
        DateTimeOffset? capturedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeframe);
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);

        var bars = new List<MarketBar>();
        using var reader = new StringReader(text);
        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("Date", StringComparison.OrdinalIgnoreCase)) continue;

            var fields = line.Split(';');
            if (fields.Length < 6)
                throw new FormatException($"OHLCV line {lineNumber} contains fewer than 6 fields.");

            if (!DateTime.TryParseExact(fields[0].Trim(), new[] { "yyyyMMdd HHmmss", "yyyyMMdd HHmm" },
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
                throw new FormatException($"Invalid timestamp at line {lineNumber}: '{fields[0]}'.");

            var open = ParseDecimal(fields[1], lineNumber);
            var high = ParseDecimal(fields[2], lineNumber);
            var low = ParseDecimal(fields[3], lineNumber);
            var close = ParseDecimal(fields[4], lineNumber);
            var volume = ParseLong(fields[5], lineNumber);

            if (high < Math.Max(open, close) || low > Math.Min(open, close) || low > high)
                throw new FormatException($"Invalid OHLC relationship at line {lineNumber}.");

            bars.Add(new MarketBar(new DateTimeOffset(timestamp), open, high, low, close, volume));
        }

        bars.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        for (var i = 1; i < bars.Count; i++)
        {
            if (bars[i - 1].Timestamp == bars[i].Timestamp)
                throw new FormatException($"Duplicate timestamp detected: {bars[i].Timestamp:O}.");
        }

        var normalized = string.Join('\n', bars.Select(b =>
            $"{b.Timestamp:O}|{b.Open.ToString(CultureInfo.InvariantCulture)}|{b.High.ToString(CultureInfo.InvariantCulture)}|{b.Low.ToString(CultureInfo.InvariantCulture)}|{b.Close.ToString(CultureInfo.InvariantCulture)}|{b.Volume}"));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
        var captured = capturedAt ?? DateTimeOffset.UtcNow;

        return new ImportedMarketDataset(
            datasetId,
            instrument,
            timeframe,
            bars,
            new MarketDataFingerprint(datasetId, instrument, timeframe, hash, captured),
            new DataFidelityProfile(true, false, false, false, "OHLCV bars only; bid/ask, tick sequence, and replay events are not present."));
    }

    private static decimal ParseDecimal(string value, int line) =>
        decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"Invalid decimal at line {line}: '{value}'.");

    private static long ParseLong(string value, int line) =>
        long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"Invalid volume at line {line}: '{value}'.");
}
