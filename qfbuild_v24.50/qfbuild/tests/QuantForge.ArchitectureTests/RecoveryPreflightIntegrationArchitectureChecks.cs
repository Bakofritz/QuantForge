using System;
using System.IO;

namespace QuantForge.ArchitectureTests;

public static class RecoveryPreflightIntegrationArchitectureChecks
{
    public static void Validate()
    {
        var runtime = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "QuantForge.Runtime", "ResearchRuntime.cs"));
        var continuation = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "QuantForge.Runtime", "RecoveryContinuationService.cs"));
        var checkpoint = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "QuantForge.Runtime", "CheckpointBoundContinuationService.cs"));
        if (!runtime.Contains("IRecoveryPreflightStore", StringComparison.Ordinal)) throw new InvalidOperationException("Runtime preflight store integration missing.");
        if (!runtime.Contains("LoadRecoveryPreflightsAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Runtime preflight reader missing.");
        if (!continuation.Contains("_preflight", StringComparison.Ordinal) || !continuation.Contains("RecordAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Recovery continuation preflight binding missing.");
        if (!checkpoint.Contains("_preflightStore", StringComparison.Ordinal)) throw new InvalidOperationException("Checkpoint continuation preflight propagation missing.");
    }
}
