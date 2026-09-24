namespace QuantForge.Governance.ArchitectureChecks;
public static class ConsolidatedReleaseArchitectureChecksV24_10
{
    public static bool RequiresAllResearchGates() => !QuantForge.Backtesting.ConsolidatedResearchReleaseGateV24_10.CanPublish(new(true,true,true,true,true,true,true,true,true,true));
}
