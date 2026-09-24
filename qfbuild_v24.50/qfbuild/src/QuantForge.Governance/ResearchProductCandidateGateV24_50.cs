namespace QuantForge.Governance;

public sealed record ResearchProductCandidateStateV24_50(bool AcceptanceGatePassed, bool RegressionBaselinePassed, bool AndroidHarnessReady, bool DocumentationComplete, bool LiveBrokerageDisabled, bool LiveOrderSubmissionDisabled, bool ReadOnlyData, bool SimulationOnly);
public static class ResearchProductCandidateGateV24_50
{
    public static bool CanCandidateRelease(ResearchProductCandidateStateV24_50? s) => s is not null && s.AcceptanceGatePassed && s.RegressionBaselinePassed && s.AndroidHarnessReady && s.DocumentationComplete && s.LiveBrokerageDisabled && s.LiveOrderSubmissionDisabled && s.ReadOnlyData && s.SimulationOnly;
}
