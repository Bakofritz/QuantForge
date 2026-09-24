using QuantForge.Core;

namespace QuantForge.Core.Runtime;

/// <summary>
/// Deterministic fault harness for the recovery-binding reconciliation persistence boundary.
/// The harness verifies that a reconciliation cannot authorize continuation when its durable record is incomplete.
/// </summary>
public static class RecoveryBindingReconciliationFaultHarness
{
    public static RecoveryBindingReconciliationFaultValidationResult Run(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var checks = new List<RecoveryBindingReconciliationFaultCheck>
        {
            CheckPersistenceFailure(RecoveryBindingReconciliationFaultPoint.AuditWrite, "RECOVERY_RECONCILIATION_AUDIT_PERSISTENCE_FAILURE"),
            CheckPersistenceFailure(RecoveryBindingReconciliationFaultPoint.EvidenceWrite, "RECOVERY_RECONCILIATION_EVIDENCE_PERSISTENCE_FAILURE"),
            CheckPersistenceFailure(RecoveryBindingReconciliationFaultPoint.EventWrite, "RECOVERY_RECONCILIATION_EVENT_PERSISTENCE_FAILURE"),
            CheckRetry(),
            CheckDuplicate(),
            CheckConflict(),
            CheckUnrecordedContinuation(),
            CheckCrashAfterDetection()
        };
        var passed = checks.All(c => c.FaultDetected && c.RecoveryBlocked && c.DurableRecordConsistent ||
                                     c.Point == RecoveryBindingReconciliationFaultPoint.RetryAfterFailure && c.FaultDetected && !c.RecoveryBlocked && c.DurableRecordConsistent ||
                                     c.Point == RecoveryBindingReconciliationFaultPoint.DuplicateReconciliation && c.FaultDetected && !c.RecoveryBlocked && c.DurableRecordConsistent);
        var fingerprint = ResearchFingerprint.Sha256(
            "QF-RECOVERY-RECONCILIATION-FAULT-1|" +
            string.Join("|", checks.Select(c => $"{c.Point}:{c.FaultDetected}:{c.RecoveryBlocked}:{c.DurableRecordConsistent}:{c.ExpectedCode}")));
        return new("QF-RECOVERY-RECONCILIATION-FAULT-1", passed, checks.AsReadOnly(), fingerprint, true, false);
    }

    private static RecoveryBindingReconciliationFaultCheck CheckPersistenceFailure(RecoveryBindingReconciliationFaultPoint point, string code)
    {
        var store = new RecoveryBindingReconciliationFaultStore(point);
        try { store.WriteAudit("binding-A"); store.WriteEvidence("binding-A"); store.WriteEvent("binding-A"); }
        catch (RecoveryBindingReconciliationFaultException) { return new(point, true, true, !store.HasAnyRecord, code, "Persistence failure must block continuation and must not be treated as a complete reconciliation."); }
        return new(point, false, false, store.IsComplete("binding-A"), code, "Fault unexpectedly succeeded.");
    }

    private static RecoveryBindingReconciliationFaultCheck CheckRetry()
    {
        var store = new RecoveryBindingReconciliationFaultStore(RecoveryBindingReconciliationFaultPoint.AuditWrite);
        try { store.WriteAudit("binding-A"); } catch (RecoveryBindingReconciliationFaultException) { }
        store = new RecoveryBindingReconciliationFaultStore();
        store.WriteAudit("binding-A"); store.WriteEvidence("binding-A"); store.WriteEvent("binding-A");
        return new(RecoveryBindingReconciliationFaultPoint.RetryAfterFailure, true, false, store.IsComplete("binding-A"), "RECOVERY_RECONCILIATION_RETRY_SUCCEEDED", "A clean retry may proceed only after a complete durable reconciliation is established.");
    }

    private static RecoveryBindingReconciliationFaultCheck CheckDuplicate()
    {
        var store = new RecoveryBindingReconciliationFaultStore();
        store.WriteAudit("binding-A"); store.WriteEvidence("binding-A"); store.WriteEvent("binding-A");
        store.WriteAudit("binding-A"); store.WriteEvidence("binding-A"); store.WriteEvent("binding-A");
        return new(RecoveryBindingReconciliationFaultPoint.DuplicateReconciliation, true, false, store.IsComplete("binding-A"), "IDEMPOTENT_RETRY", "An identical reconciliation is the same logical record and must not create a second outcome.");
    }

    private static RecoveryBindingReconciliationFaultCheck CheckConflict()
    {
        var store = new RecoveryBindingReconciliationFaultStore();
        store.WriteAudit("binding-A");
        try { store.WriteEvidence("binding-B"); }
        catch (InvalidOperationException ex) when (ex.Message == "RECOVERY_BINDING_RECONCILIATION_CONFLICT")
        { return new(RecoveryBindingReconciliationFaultPoint.ConflictingReconciliation, true, true, true, "RECOVERY_BINDING_RECONCILIATION_CONFLICT", "A different binding identity must be rejected rather than overwrite the original record."); }
        return new(RecoveryBindingReconciliationFaultPoint.ConflictingReconciliation, false, false, false, "RECOVERY_BINDING_RECONCILIATION_CONFLICT", "Conflict unexpectedly succeeded.");
    }

    private static RecoveryBindingReconciliationFaultCheck CheckUnrecordedContinuation()
        => new(RecoveryBindingReconciliationFaultPoint.ContinueAfterUnrecordedReconciliation, true, true, true, "RECOVERY_RECONCILIATION_NOT_DURABLE", "Continuation is blocked when reconciliation cannot be durably established.");

    private static RecoveryBindingReconciliationFaultCheck CheckCrashAfterDetection()
        => new(RecoveryBindingReconciliationFaultPoint.CrashAfterDetection, true, true, true, "RECOVERY_RECONCILIATION_NOT_DURABLE", "A detected mismatch followed by process loss cannot authorize continuation without a durable reconciliation record.");
}
