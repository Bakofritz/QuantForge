using QuantForge.Core;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class RecoveryContinuationArchitectureChecks
{
    public static void Run()
    {
        if (!Enum.GetValues<RecoveryContinuationState>().Contains(RecoveryContinuationState.Ready))
            throw new InvalidOperationException("RECOVERY_READY_STATE_MISSING");
        if (!Enum.GetValues<RecoveryContinuationState>().Contains(RecoveryContinuationState.CheckpointFingerprintMismatch))
            throw new InvalidOperationException("CHECKPOINT_FINGERPRINT_GATE_MISSING");
        if (!typeof(IResearchJobStore).GetMethod(nameof(IResearchJobStore.LoadLatestCheckpointAsync))!.ReturnType.Name.Contains("Task"))
            throw new InvalidOperationException("CHECKPOINT_LOAD_MISSING");
        if (!typeof(IJobLeaseStore).GetMethod(nameof(IJobLeaseStore.TryAcquireAsync))!.ReturnType.Name.Contains("Task"))
            throw new InvalidOperationException("RECOVERY_LEASE_ACQUIRE_MISSING");
        if (!typeof(IResourceReceiptStore).GetMethod(nameof(IResourceReceiptStore.LoadExecutionReceiptsForJobAsync))!.ReturnType.Name.Contains("Task"))
            throw new InvalidOperationException("TERMINAL_RECEIPT_GATE_MISSING");
        if (!typeof(IRecoveryAuditStore).GetMethod(nameof(IRecoveryAuditStore.AppendRecoveryAuditAsync))!.ReturnType.Name.Contains("Task"))
            throw new InvalidOperationException("RECOVERY_AUDIT_MISSING");
        if (!typeof(RecoveryContinuationService).GetMethod(nameof(RecoveryContinuationService.PrepareAsync))!.ReturnType.Name.Contains("Task"))
            throw new InvalidOperationException("RECOVERY_PREPARATION_MISSING");
        if (!ResearchJobLifecycle.CheckpointMatches(
                new ResearchJob("j", DurableJobState.RecoveryRequired, "d", "c", "e", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                new ResearchCheckpoint("j", 1, 0, "d", "c", "e", "state", DateTimeOffset.UtcNow)))
            throw new InvalidOperationException("CHECKPOINT_IDENTITY_MATCH_FAILED");
    }
}
