namespace QuantForge.Backtesting;

public sealed record ResearchRegressionBaselineV24_40(string BaselineId, string DatasetFingerprint, string EngineFingerprint, string ResultFingerprint, bool ReplayCertified, bool ReadOnlyData, bool SimulationOnly);
public static class ResearchRegressionBaselineGateV24_40
{
    public static bool Passes(ResearchRegressionBaselineV24_40? b) => b is not null && !string.IsNullOrWhiteSpace(b.BaselineId) && !string.IsNullOrWhiteSpace(b.DatasetFingerprint) && !string.IsNullOrWhiteSpace(b.EngineFingerprint) && !string.IsNullOrWhiteSpace(b.ResultFingerprint) && b.ReplayCertified && b.ReadOnlyData && b.SimulationOnly;
}
