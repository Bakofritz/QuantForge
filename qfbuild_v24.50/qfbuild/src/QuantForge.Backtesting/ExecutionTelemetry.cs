using QuantForge.Core;

namespace QuantForge.Backtesting;

public enum ExecutionTelemetryKind { ModeledAssumption, MeasuredObservation }

public sealed record ExecutionTelemetryPoint(
    DateTimeOffset Timestamp,
    ExecutionTelemetryKind Kind,
    int RoundTripLatencyMs,
    decimal SlippageTicksPerSide,
    string ConnectionProfile,
    string Source,
    string Fingerprint);

/// <summary>Stores research telemetry without treating estimates as measurements.</summary>
public static class ExecutionTelemetry
{
    public static ExecutionTelemetryPoint FromModeledScenario(
        DateTimeOffset timestamp, ExecutionScenario scenario) =>
        new(timestamp, ExecutionTelemetryKind.ModeledAssumption, scenario.LatencyMs,
            scenario.SlippageTicksPerSide, scenario.ConnectionProfile,
            "governed-sensitivity-scenario",
            ResearchFingerprint.Sha256($"MODELED|{timestamp:O}|{scenario.ScenarioId}|{scenario.LatencyMs}|{scenario.SlippageTicksPerSide}|{scenario.ConnectionProfile}"));

    public static ExecutionTelemetryPoint FromMeasuredObservation(
        DateTimeOffset timestamp, int roundTripLatencyMs, decimal slippageTicksPerSide,
        string connectionProfile, string source) {
        if (roundTripLatencyMs < 0 || slippageTicksPerSide < 0m)
            throw new ArgumentOutOfRangeException(nameof(roundTripLatencyMs));
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new(timestamp, ExecutionTelemetryKind.MeasuredObservation,
            roundTripLatencyMs, slippageTicksPerSide, connectionProfile, source,
            ResearchFingerprint.Sha256($"MEASURED|{timestamp:O}|{roundTripLatencyMs}|{slippageTicksPerSide}|{connectionProfile}|{source}"));
    }
}
