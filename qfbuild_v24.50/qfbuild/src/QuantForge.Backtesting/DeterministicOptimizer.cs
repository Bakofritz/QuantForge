using System.Security.Cryptography;
using System.Text;
using QuantForge.Data;

namespace QuantForge.Backtesting;

/// <summary>Deterministic, isolated grid-search runner. It reports every trial and never mutates source data.</summary>
public sealed class DeterministicOptimizer
{
    private readonly EmaCrossBacktestEngine _engine = new();

    public OptimizationReport Run(
        string jobId,
        IReadOnlyList<MarketBar> bars,
        string datasetFingerprint,
        string instrument,
        string timeframe,
        string engineVersion,
        ParameterRange fastRange,
        ParameterRange slowRange,
        decimal pointValue = 1m,
        decimal commissionPoints = 0m)
    {
        ValidateRange(fastRange, nameof(fastRange));
        ValidateRange(slowRange, nameof(slowRange));
        if (fastRange.Min >= slowRange.Max) throw new ArgumentException("Parameter ranges must contain at least one valid fast/slow combination.");

        var cases = new List<OptimizationCaseResult>();
        foreach (var fast in Values(fastRange))
        foreach (var slow in Values(slowRange))
        {
            if (fast >= slow) continue;
            var config = Hash($"EMA_CROSS|fast={fast}|slow={slow}|point={pointValue}|commission={commissionPoints}|engine={engineVersion}");
            var request = new BacktestRequest("EMA_CROSS", instrument, timeframe, datasetFingerprint, engineVersion);
            var result = _engine.Run(bars, request, fast, slow, pointValue, commissionPoints);
            cases.Add(new OptimizationCaseResult(
                new OptimizationCase($"{jobId}:{fast}:{slow}", fast, slow, datasetFingerprint, config),
                result.Metrics,
                result.Result.ResultHash));
        }

        var searchFingerprint = Hash(string.Join("|", cases.Select(x => x.Case.ConfigurationFingerprint + ":" + x.ResultHash)));
        return new OptimizationReport(jobId, cases, searchFingerprint,
            "OHLCV bar-driven simulation; optimization does not provide tick ordering, bid/ask, spread, or intrabar execution fidelity.");
    }

    public IReadOnlyList<OptimizationCaseResult> RunChunk(
        IReadOnlyList<OptimizationCase> cases, IReadOnlyList<MarketBar> bars, string instrument, string timeframe,
        string engineVersion, decimal pointValue = 1m, decimal commissionPoints = 0m,
        int startIndex = 0, int maxCases = 100)
    {
        if (startIndex < 0 || maxCases <= 0) throw new ArgumentOutOfRangeException();
        var results = new List<OptimizationCaseResult>();
        foreach (var item in cases.Skip(startIndex).Take(maxCases))
        {
            var request = new BacktestRequest("EMA_CROSS", instrument, timeframe, item.DatasetFingerprint, engineVersion);
            var result = _engine.Run(bars, request, item.FastPeriod, item.SlowPeriod, pointValue, commissionPoints);
            results.Add(new OptimizationCaseResult(item, result.Metrics, result.Result.ResultHash));
        }
        return results;
    }

    private static IEnumerable<int> Values(ParameterRange r)
    {
        for (var value = r.Min; value <= r.Max; value += r.Step) yield return value;
    }

    private static void ValidateRange(ParameterRange range, string name)
    {
        ArgumentNullException.ThrowIfNull(range, name);
        if (string.IsNullOrWhiteSpace(range.Name) || range.Min <= 0 || range.Max < range.Min || range.Step <= 0)
            throw new ArgumentException("Invalid parameter range.", name);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
