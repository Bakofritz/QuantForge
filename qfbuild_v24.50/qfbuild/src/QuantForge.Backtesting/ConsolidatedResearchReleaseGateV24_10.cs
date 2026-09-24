namespace QuantForge.Backtesting;

public sealed record ConsolidatedResearchReleaseStateV24_10(bool ParameterConstraintsValid, bool ParetoAnalysisValid, bool PortfolioSimulationValid, bool RiskScenarioValid, bool ProvenanceValid, bool StabilityGateValid, bool ReplayCertified, bool ReadOnlyData, bool SimulationOnly, bool LiveAuthority);
public static class ConsolidatedResearchReleaseGateV24_10
{
    public static bool CanPublish(ConsolidatedResearchReleaseStateV24_10 s) => s is not null && s.ParameterConstraintsValid && s.ParetoAnalysisValid && s.PortfolioSimulationValid && s.RiskScenarioValid && s.ProvenanceValid && s.StabilityGateValid && s.ReplayCertified && s.ReadOnlyData && s.SimulationOnly && !s.LiveAuthority;
}
