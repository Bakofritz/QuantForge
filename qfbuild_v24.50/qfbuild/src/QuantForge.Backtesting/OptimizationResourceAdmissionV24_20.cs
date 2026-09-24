namespace QuantForge.Backtesting;

public sealed record OptimizationResourceRequestV24_20(int CandidateCount, int MaxConcurrentJobs, long MaxEstimatedWorkUnits, bool ReadOnlyData, bool SimulationOnly);
public static class OptimizationResourceAdmissionV24_20
{
    public static bool Admit(OptimizationResourceRequestV24_20? r) => r is not null && r.CandidateCount > 0 && r.MaxConcurrentJobs > 0 && r.MaxEstimatedWorkUnits > 0 && r.MaxConcurrentJobs <= r.CandidateCount && r.ReadOnlyData && r.SimulationOnly;
}
