namespace QuantForge.Governance.ArchitectureChecks;
public static class RiskScenarioArchitectureChecksV23_90
{
    public static bool ReadOnlyOnly() => QuantForge.Backtesting.RiskScenarioAnalysisGateV23_90.CanRun(new("d", new List<QuantForge.Backtesting.RiskScenarioV23_90>{new("base",0,1,0)}, true, true, false));
}
