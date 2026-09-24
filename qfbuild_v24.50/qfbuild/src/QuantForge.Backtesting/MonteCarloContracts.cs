namespace QuantForge.Backtesting;

public sealed record MonteCarloSummary(
    int Iterations,
    int TradesPerIteration,
    decimal ObservedNetPnlPoints,
    decimal MeanNetPnlPoints,
    decimal MedianNetPnlPoints,
    decimal P05NetPnlPoints,
    decimal P95NetPnlPoints,
    decimal MeanMaxDrawdownPoints,
    decimal P95MaxDrawdownPoints,
    decimal ProbabilityNonPositivePnl,
    decimal ProbabilityDrawdownAtLeastObserved,
    string SeedFingerprint);

public sealed record MonteCarloReport(
    string JobId,
    string BaselineResultHash,
    MonteCarloSummary Summary,
    string ResamplingMethod,
    string DataFidelityDeclaration,
    string ReportFingerprint);
