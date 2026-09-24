namespace QuantForge.Backtesting;

public sealed record ParameterRange(string Name, int Min, int Max, int Step);

public sealed record OptimizationCase(string CaseId, int FastPeriod, int SlowPeriod, string DatasetFingerprint, string ConfigurationFingerprint);
public sealed record OptimizationCaseResult(OptimizationCase Case, BacktestMetrics Metrics, string ResultHash);
public sealed record OptimizationReport(string JobId, IReadOnlyList<OptimizationCaseResult> Cases, string SearchFingerprint, string DataFidelityDeclaration);

public sealed record WalkForwardWindow(int WindowId, int TrainStart, int TrainEndExclusive, int TestStart, int TestEndExclusive);
public sealed record WalkForwardCaseResult(WalkForwardWindow Window, int FastPeriod, int SlowPeriod, BacktestMetrics Training, BacktestMetrics OutOfSample);
public sealed record WalkForwardReport(string JobId, IReadOnlyList<WalkForwardCaseResult> Windows, string ConfigurationFingerprint, string DataFidelityDeclaration);
