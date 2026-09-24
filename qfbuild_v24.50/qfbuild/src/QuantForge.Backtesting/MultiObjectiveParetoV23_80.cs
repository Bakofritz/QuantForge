namespace QuantForge.Backtesting;

public sealed record ObjectiveValueV23_80(string Name, double Value, bool Maximize);
public sealed record ParetoPointV23_80(string RunId, IReadOnlyList<ObjectiveValueV23_80> Objectives, string ResultFingerprint);
public static class MultiObjectiveParetoV23_80
{
    public static bool Dominates(ParetoPointV23_80 a, ParetoPointV23_80 b) => a.Objectives.Count == b.Objectives.Count && a.Objectives.Zip(b.Objectives).All(p => p.First.Maximize ? p.First.Value >= p.Second.Value : p.First.Value <= p.Second.Value) && a.Objectives.Zip(b.Objectives).Any(p => p.First.Maximize ? p.First.Value > p.Second.Value : p.First.Value < p.Second.Value);
    public static bool IsResearchOnly(bool readOnlyData, bool simulationOnly, bool liveAuthority) => readOnlyData && simulationOnly && !liveAuthority;
}
