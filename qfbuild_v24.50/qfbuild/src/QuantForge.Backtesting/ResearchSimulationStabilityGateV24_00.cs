namespace QuantForge.Backtesting;

public sealed record ResearchSimulationStabilityStateV24_00(bool ReadOnlyData, bool SimulationOnly, bool LiveAuthority, bool StrategyGovernanceValid, bool DatasetIntegrityValid, bool ParameterSpaceValid, bool DeterministicFingerprintValid, bool CancellationSafe);
public static class ResearchSimulationStabilityGateV24_00
{
    public static bool CanReleaseResearchResult(ResearchSimulationStabilityStateV24_00 s) => s is not null && s.ReadOnlyData && s.SimulationOnly && !s.LiveAuthority && s.StrategyGovernanceValid && s.DatasetIntegrityValid && s.ParameterSpaceValid && s.DeterministicFingerprintValid && s.CancellationSafe;
}
