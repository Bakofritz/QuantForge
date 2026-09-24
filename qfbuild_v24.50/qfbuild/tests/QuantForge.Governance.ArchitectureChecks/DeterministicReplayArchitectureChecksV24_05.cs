namespace QuantForge.Governance.ArchitectureChecks;
public static class DeterministicReplayArchitectureChecksV24_05
{
    public static bool RequiresMultipleReplays() => !QuantForge.Backtesting.DeterministicReplayGateV24_05.IsCertified(new("r","i","x",1,true));
}
