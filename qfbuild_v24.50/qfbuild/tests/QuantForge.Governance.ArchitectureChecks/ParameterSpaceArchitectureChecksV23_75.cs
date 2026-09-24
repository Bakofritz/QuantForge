namespace QuantForge.Governance.ArchitectureChecks;

public static class ParameterSpaceArchitectureChecksV23_75
{
    public static bool PreservesResearchBoundary() => QuantForge.Backtesting.AdvancedParameterSpaceGateV23_75.CanOptimize(new(new List<QuantForge.Backtesting.ParameterConstraintV23_75>(), "placeholder"), true, true, false) == false;
}
