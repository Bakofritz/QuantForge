namespace QuantForge.Governance;

public sealed record ResearchAcceptanceChecklistV24_45(bool ImportGovernance, bool DataIntegrity, bool ParameterConstraints, bool OptimizationValidated, bool PortfolioValidated, bool RiskValidated, bool ProvenanceValidated, bool ReplayValidated, bool RegressionValidated, bool ReadOnlyData, bool SimulationOnly, bool LiveAuthority);
public static class ResearchAcceptanceChecklistGateV24_45
{
    public static bool Passes(ResearchAcceptanceChecklistV24_45? c) => c is not null && c.ImportGovernance && c.DataIntegrity && c.ParameterConstraints && c.OptimizationValidated && c.PortfolioValidated && c.RiskValidated && c.ProvenanceValidated && c.ReplayValidated && c.RegressionValidated && c.ReadOnlyData && c.SimulationOnly && !c.LiveAuthority;
}
