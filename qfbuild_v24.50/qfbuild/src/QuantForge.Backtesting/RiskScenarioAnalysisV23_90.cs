namespace QuantForge.Backtesting;

public sealed record RiskScenarioV23_90(string ScenarioId, double ReturnShock, double VolatilityMultiplier, double CorrelationShift);
public sealed record RiskAnalysisInputV23_90(string DatasetFingerprint, IReadOnlyList<RiskScenarioV23_90> Scenarios, bool ReadOnlyData, bool SimulationOnly, bool LiveAuthority);
public static class RiskScenarioAnalysisGateV23_90
{
    public static bool CanRun(RiskAnalysisInputV23_90 input) => input is not null && !string.IsNullOrWhiteSpace(input.DatasetFingerprint) && input.Scenarios.Count > 0 && input.Scenarios.All(s => !string.IsNullOrWhiteSpace(s.ScenarioId) && s.VolatilityMultiplier >= 0) && input.ReadOnlyData && input.SimulationOnly && !input.LiveAuthority;
}
