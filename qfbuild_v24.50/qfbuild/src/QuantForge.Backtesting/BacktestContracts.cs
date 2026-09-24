namespace QuantForge.Backtesting;

public sealed record BacktestRequest(string StrategyId, string Instrument, string Timeframe, string DatasetFingerprint, string EngineVersion);
public sealed record BacktestResult(string RunId, string RequestHash, string ResultHash, bool Completed, string DataFidelityDeclaration)
{
    public string FidelityDeclaration => DataFidelityDeclaration;
}

public interface IBacktestEngine
{
    Task<BacktestResult> RunAsync(BacktestRequest request, CancellationToken cancellationToken = default);
}
