namespace QuantForge.Reporting;

public sealed record ResearchResultBundleV24_30(string RunId, string DatasetFingerprint, string StrategyFingerprint, string ResultFingerprint, string ProvenanceFingerprint, bool ReadOnlyData, bool SimulationOnly);
public static class ResearchResultBundleIntegrityV24_30
{
    public static bool IsPublishable(ResearchResultBundleV24_30? b) => b is not null && !string.IsNullOrWhiteSpace(b.RunId) && !string.IsNullOrWhiteSpace(b.DatasetFingerprint) && !string.IsNullOrWhiteSpace(b.StrategyFingerprint) && !string.IsNullOrWhiteSpace(b.ResultFingerprint) && !string.IsNullOrWhiteSpace(b.ProvenanceFingerprint) && b.ReadOnlyData && b.SimulationOnly;
}
