namespace QuantForge.Backtesting;

public sealed record ScenarioCaseV24_25(string ScenarioId, string DatasetFingerprint, string ParameterFingerprint, int Seed, string CaseFingerprint);
public static class DeterministicScenarioMatrixV24_25
{
    public static bool IsDeterministic(ScenarioCaseV24_25? c) => c is not null && !string.IsNullOrWhiteSpace(c.ScenarioId) && !string.IsNullOrWhiteSpace(c.DatasetFingerprint) && !string.IsNullOrWhiteSpace(c.ParameterFingerprint) && !string.IsNullOrWhiteSpace(c.CaseFingerprint);
    public static string BuildReplayKey(ScenarioCaseV24_25 c) => $"{c.ScenarioId}|{c.DatasetFingerprint}|{c.ParameterFingerprint}|{c.Seed}";
}
