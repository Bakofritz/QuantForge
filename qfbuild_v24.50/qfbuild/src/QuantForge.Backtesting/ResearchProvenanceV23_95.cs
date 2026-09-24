namespace QuantForge.Backtesting;

public sealed record ResearchProvenanceV23_95(string RunId, string StrategyFingerprint, string DatasetFingerprint, string ParameterFingerprint, string EngineFingerprint, string ResultFingerprint);
public static class ResearchReproducibilityGateV23_95
{
    public static bool IsReproducible(ResearchProvenanceV23_95 p) => p is not null && new[]{p.RunId,p.StrategyFingerprint,p.DatasetFingerprint,p.ParameterFingerprint,p.EngineFingerprint,p.ResultFingerprint}.All(x => !string.IsNullOrWhiteSpace(x));
}
