using QuantForge.Data;

namespace QuantForge.Backtesting;

/// <summary>Chronological train/test evaluator. Parameters selected from each train window are evaluated only on its subsequent test window.</summary>
public sealed class WalkForwardAnalyzer
{
    private readonly DeterministicOptimizer _optimizer = new();
    private readonly EmaCrossBacktestEngine _engine = new();

    public WalkForwardReport Run(
        string jobId,
        IReadOnlyList<MarketBar> bars,
        string datasetFingerprint,
        string instrument,
        string timeframe,
        string engineVersion,
        int trainBars,
        int testBars,
        int stepBars,
        ParameterRange fastRange,
        ParameterRange slowRange,
        decimal pointValue = 1m,
        decimal commissionPoints = 0m)
    {
        if (trainBars <= 0 || testBars <= 0 || stepBars <= 0) throw new ArgumentOutOfRangeException(nameof(trainBars));
        if (bars.Count < trainBars + testBars) throw new ArgumentException("Dataset is too short for one walk-forward window.", nameof(bars));
        for (var i = 1; i < bars.Count; i++)
            if (bars[i - 1].Timestamp >= bars[i].Timestamp) throw new ArgumentException("Bars must be strictly chronological.", nameof(bars));

        var windows = new List<WalkForwardCaseResult>();
        var windowId = 0;
        for (var start = 0; start + trainBars + testBars <= bars.Count; start += stepBars)
        {
            var train = bars.Skip(start).Take(trainBars).ToList();
            var test = bars.Skip(start + trainBars).Take(testBars).ToList();
            var trainFingerprint = datasetFingerprint + $":train:{start}:{trainBars}";
            var report = _optimizer.Run($"{jobId}:train:{windowId}", train, trainFingerprint, instrument, timeframe, engineVersion,
                fastRange, slowRange, pointValue, commissionPoints);
            var selected = report.Cases
                .OrderByDescending(x => x.Metrics.NetPnlPoints)
                .ThenBy(x => x.Metrics.MaxDrawdownPoints)
                .ThenBy(x => x.Case.FastPeriod)
                .ThenBy(x => x.Case.SlowPeriod)
                .First();

            var testRequest = new BacktestRequest("EMA_CROSS", instrument, timeframe,
                datasetFingerprint + $":test:{start + trainBars}:{testBars}", engineVersion);
            var outOfSample = _engine.Run(test, testRequest, selected.Case.FastPeriod, selected.Case.SlowPeriod,
                pointValue, commissionPoints);
            windows.Add(new WalkForwardCaseResult(
                new WalkForwardWindow(windowId++, start, start + trainBars, start + trainBars, start + trainBars + testBars),
                selected.Case.FastPeriod, selected.Case.SlowPeriod, selected.Metrics, outOfSample.Metrics));
        }

        var fingerprint = string.Join("|", windows.Select(x => $"{x.Window.WindowId}:{x.FastPeriod}:{x.SlowPeriod}:{x.Training.NetPnlPoints}:{x.OutOfSample.NetPnlPoints}"));
        return new WalkForwardReport(jobId, windows, fingerprint,
            "Chronological train/test evaluation over OHLCV bar data; parameter selection is isolated to each training window and is not performed on the following test window.");
    }
}
