using System.Security.Cryptography;
using System.Text;
using QuantForge.Data;

namespace QuantForge.Backtesting;

/// <summary>Deterministic local perturbation analysis around a selected parameter set. It never changes canonical data.</summary>
public sealed class DeterministicRobustnessAnalyzer
{
    private readonly EmaCrossBacktestEngine _engine = new();

    public RobustnessReport Run(
        string jobId, IReadOnlyList<MarketBar> bars, string datasetFingerprint, string instrument, string timeframe,
        string engineVersion, int baselineFast, int baselineSlow, int radius, decimal pointValue = 1m, decimal commissionPoints = 0m)
    {
        if (radius < 0 || radius > 10) throw new ArgumentOutOfRangeException(nameof(radius));
        if (baselineFast <= 0 || baselineSlow <= baselineFast) throw new ArgumentException("Baseline EMA parameters are invalid.");
        if (bars.Count == 0) throw new ArgumentException("At least one bar is required.", nameof(bars));
        var baselineRequest = new BacktestRequest("EMA_CROSS", instrument, timeframe, datasetFingerprint, engineVersion);
        var baseline = _engine.Run(bars, baselineRequest, baselineFast, baselineSlow, pointValue, commissionPoints);
        var cases = new List<RobustnessCaseResult>();
        for (var fast = Math.Max(1, baselineFast - radius); fast <= baselineFast + radius; fast++)
        for (var slow = Math.Max(fast + 1, baselineSlow - radius); slow <= baselineSlow + radius; slow++)
        {
            var result = _engine.Run(bars, baselineRequest, fast, slow, pointValue, commissionPoints);
            cases.Add(new RobustnessCaseResult(new ParameterPerturbation($"fast={fast},slow={slow}", fast, slow), result.Metrics, result.Result.ResultHash));
        }
        var validPnl = cases.Select(x => x.Metrics.NetPnlPoints).Where(x => x != 0m).ToList();
        var pnlRetention = baseline.Metrics.NetPnlPoints == 0m ? 0m : cases.Count == 0 ? 0m : cases.Average(x => x.Metrics.NetPnlPoints) / baseline.Metrics.NetPnlPoints;
        var avgDd = cases.Count == 0 ? 0m : cases.Average(x => x.Metrics.MaxDrawdownPoints);
        var ddRetention = baseline.Metrics.MaxDrawdownPoints == 0m ? 0m : baseline.Metrics.MaxDrawdownPoints / Math.Max(avgDd, 0.000001m);
        var fingerprint = Hash(string.Join("|", cases.Select(x => $"{x.Perturbation.FastPeriod}:{x.Perturbation.SlowPeriod}:{x.ResultHash}")));
        return new RobustnessReport(jobId, baseline.Result.ResultHash, cases, pnlRetention, ddRetention, fingerprint,
            "OHLCV bar-driven perturbation analysis; it does not create tick ordering, bid/ask, spread, or intrabar execution fidelity.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
