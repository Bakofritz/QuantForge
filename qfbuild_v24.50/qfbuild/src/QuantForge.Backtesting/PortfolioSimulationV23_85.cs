namespace QuantForge.Backtesting;

public sealed record StrategyAllocationV23_85(string StrategyId, double Weight);
public sealed record PortfolioSimulationRequestV23_85(string PortfolioId, IReadOnlyList<StrategyAllocationV23_85> Allocations, string DatasetFingerprint, string RunFingerprint, bool ReadOnlyData, bool SimulationOnly, bool LiveAuthority);
public static class PortfolioSimulationGateV23_85
{
    public static bool CanRun(PortfolioSimulationRequestV23_85 request) => request is not null && !string.IsNullOrWhiteSpace(request.PortfolioId) && !string.IsNullOrWhiteSpace(request.DatasetFingerprint) && !string.IsNullOrWhiteSpace(request.RunFingerprint) && request.Allocations.Count > 0 && request.Allocations.All(a => !string.IsNullOrWhiteSpace(a.StrategyId) && a.Weight >= 0) && request.ReadOnlyData && request.SimulationOnly && !request.LiveAuthority;
}
