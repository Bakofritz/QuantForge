namespace QuantForge.Backtesting;

public sealed record ParameterConstraintV23_75(string Name, double? Min, double? Max, double? Step, IReadOnlySet<string> AllowedValues);
public sealed record ParameterSpaceV23_75(IReadOnlyList<ParameterConstraintV23_75> Constraints, string SpaceFingerprint)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(SpaceFingerprint) && Constraints.All(c => !string.IsNullOrWhiteSpace(c.Name) && (c.AllowedValues.Count > 0 || (c.Min.HasValue && c.Max.HasValue && c.Step.HasValue && c.Step.Value > 0 && c.Max.Value >= c.Min.Value)));
}
public static class AdvancedParameterSpaceGateV23_75
{
    public static bool CanOptimize(ParameterSpaceV23_75 space, bool readOnlyData, bool simulationOnly, bool liveAuthority) => space is not null && space.IsValid() && readOnlyData && simulationOnly && !liveAuthority;
}
