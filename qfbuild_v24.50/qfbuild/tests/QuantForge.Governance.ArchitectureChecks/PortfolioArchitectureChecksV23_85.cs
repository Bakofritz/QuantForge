namespace QuantForge.Governance.ArchitectureChecks;
public static class PortfolioArchitectureChecksV23_85
{
    public static bool RejectsLiveAuthority() => !QuantForge.Backtesting.PortfolioSimulationGateV23_85.CanRun(new("p", new List<QuantForge.Backtesting.StrategyAllocationV23_85>{new("s",1)}, "d", "r", true, true, true));
}
