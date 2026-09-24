namespace QuantForge.Governance.ArchitectureChecks;
public static class ParetoArchitectureChecksV23_80
{
    public static bool ResearchOnly() => QuantForge.Backtesting.MultiObjectiveParetoV23_80.IsResearchOnly(true, true, false);
}
