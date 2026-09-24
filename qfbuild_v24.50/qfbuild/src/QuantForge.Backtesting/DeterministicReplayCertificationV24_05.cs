namespace QuantForge.Backtesting;

public sealed record DeterministicReplayCertificateV24_05(string RunFingerprint, string InputFingerprint, string ResultFingerprint, int ReplayCount, bool AllResultsEqual);
public static class DeterministicReplayGateV24_05
{
    public static bool IsCertified(DeterministicReplayCertificateV24_05 c) => c is not null && c.ReplayCount >= 2 && c.AllResultsEqual && !string.IsNullOrWhiteSpace(c.RunFingerprint) && !string.IsNullOrWhiteSpace(c.InputFingerprint) && !string.IsNullOrWhiteSpace(c.ResultFingerprint);
}
