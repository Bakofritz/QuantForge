using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class WorkerReconciliationArchitectureChecks
{
    public static void Run()
    {
        var state = Enum.GetValues<RecoveryReconciliationState>();
        if (!state.Contains(RecoveryReconciliationState.StaleWorker)) throw new InvalidOperationException("STALE_WORKER_STATE_MISSING");
        if (!state.Contains(RecoveryReconciliationState.ReceiptAlreadyCommitted)) throw new InvalidOperationException("RECEIPT_COMMIT_STATE_MISSING");
        if (!typeof(ILeaseInspectionStore).GetMethod(nameof(ILeaseInspectionStore.LoadLeaseAsync))!.ReturnType.Name.Contains("Task")) throw new InvalidOperationException("LEASE_INSPECTION_MISSING");
        if (!typeof(IResourceReceiptStore).GetMethod(nameof(IResourceReceiptStore.LoadExecutionReceiptsForJobAsync))!.ReturnType.Name.Contains("Task")) throw new InvalidOperationException("RECEIPT_JOB_QUERY_MISSING");
        if (!typeof(IRecoveryAuditStore).GetMethod(nameof(IRecoveryAuditStore.AppendRecoveryAuditAsync))!.ReturnType.Name.Contains("Task")) throw new InvalidOperationException("RECOVERY_AUDIT_STORE_MISSING");
    }
}
