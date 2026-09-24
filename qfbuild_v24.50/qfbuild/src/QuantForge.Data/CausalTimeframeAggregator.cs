using QuantForge.Core;

namespace QuantForge.Data;

public sealed class CausalTimeframeAggregator
{
    public IReadOnlyDictionary<string, CausalBarView> Snapshot(
        IReadOnlyList<MarketBar> source,
        DateTimeOffset eventTime,
        IReadOnlyList<TimeSpan> timeframes)
    {
        var result = new Dictionary<string, CausalBarView>(StringComparer.Ordinal);
        foreach (var tf in timeframes.Distinct().Where(x => x > TimeSpan.Zero))
        {
            var eligible = source.Where(b => b.Timestamp <= eventTime).ToList();
            if (eligible.Count == 0) continue;
            var latest = eligible[^1];
            var bucketTicks = tf.Ticks;
            var bucketStartTicks = latest.Timestamp.UtcTicks - (latest.Timestamp.UtcTicks % bucketTicks);
            var bucketStart = new DateTimeOffset(bucketStartTicks, TimeSpan.Zero);
            var bucketEnd = bucketStart.Add(tf);
            var bucket = eligible.Where(b => b.Timestamp >= bucketStart && b.Timestamp < bucketEnd).ToList();
            if (bucket.Count == 0) continue;
            var complete = eventTime >= bucketEnd - TimeSpan.FromTicks(1);
            result[Format(tf)] = new CausalBarView(bucketStart, bucketEnd,
                bucket[0].Open, bucket.Max(x => x.High), bucket.Min(x => x.Low), bucket[^1].Close,
                bucket.Sum(x => x.Volume), complete);
        }
        return result;
    }

    private static string Format(TimeSpan tf) => tf.TotalMinutes >= 1
        ? $"{tf.TotalMinutes:0}m"
        : $"{tf.TotalSeconds:0}s";
}
