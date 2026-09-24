using System;
using System.IO;

namespace QuantForge.ArchitectureTests;

public static class RecoveryPreflightBindingReconciliationArchitectureChecks
{
    public static void Run(string root)
    {
        var service = File.ReadAllText(Path.Combine(root, "QuantForge.Runtime", "RecoveryPreflightBindingService.cs"));
        var continuation = File.ReadAllText(Path.Combine(root, "QuantForge.Runtime", "CheckpointBoundContinuationService.cs"));
        var runtime = File.ReadAllText(Path.Combine(root, "QuantForge.Runtime", "ResearchRuntime.cs"));
        if (!service.Contains("LoadRecoveryPreflightsForJobAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Preflight binding store read missing.");
        if (!service.Contains("CheckpointSequence", StringComparison.Ordinal) || !service.Contains("AcceptedLeaseVersion", StringComparison.Ordinal)) throw new InvalidOperationException("Checkpoint/lease binding fields missing.");
        if (!service.Contains("RECOVERY_CHECKPOINT_BINDING_MISMATCH", StringComparison.Ordinal)) throw new InvalidOperationException("Checkpoint mismatch guard missing.");
        if (!service.Contains("RECOVERY_LEASE_BINDING_MISMATCH", StringComparison.Ordinal)) throw new InvalidOperationException("Lease mismatch guard missing.");
        if (!continuation.Contains("RecoveryPreflightBindingService", StringComparison.Ordinal)) throw new InvalidOperationException("Continuation binding integration missing.");
        if (!runtime.Contains("ValidateRecoveryPreflightBindingAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Runtime binding validation entry point missing.");
    }
}
