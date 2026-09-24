namespace QuantForge.Governance.ArchitectureChecks;
public static class StabilityGateArchitectureChecksV24_00
{
    public static bool FailsClosedOnLiveAuthority() => !QuantForge.Backtesting.ResearchSimulationStabilityGateV24_00.CanReleaseResearchResult(new(true,true,true,true,true,true,true,true));
}
