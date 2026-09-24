namespace QuantForge.Backtesting;

public sealed record ParameterPerturbation(string Name, int FastPeriod, int SlowPeriod);
public sealed record RobustnessCaseResult(ParameterPerturbation Perturbation, BacktestMetrics Metrics, string ResultHash);
public sealed record RobustnessReport(string JobId, string BaselineResultHash, IReadOnlyList<RobustnessCaseResult> Cases, decimal NetPnlRetention, decimal DrawdownRetention, string StabilityFingerprint, string DataFidelityDeclaration);
