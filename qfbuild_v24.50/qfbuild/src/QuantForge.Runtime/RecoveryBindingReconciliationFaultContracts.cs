using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum RecoveryBindingReconciliationFaultPoint
{
    AuditWrite,
    EvidenceWrite,
    EventWrite,
    RetryAfterFailure,
    DuplicateReconciliation,
    ConflictingReconciliation,
    ContinueAfterUnrecordedReconciliation,
    CrashAfterDetection
}

public sealed record RecoveryBindingReconciliationFaultCheck(
    RecoveryBindingReconciliationFaultPoint Point,
    bool FaultDetected,
    bool RecoveryBlocked,
    bool DurableRecordConsistent,
    string ExpectedCode,
    string Detail);

public sealed record RecoveryBindingReconciliationFaultValidationResult(
    string HarnessVersion,
    bool Passed,
    IReadOnlyList<RecoveryBindingReconciliationFaultCheck> Checks,
    string ResultFingerprint,
    bool FailClosedExpected,
    bool NativeStorageExecuted);

public sealed class RecoveryBindingReconciliationFaultException : Exception
{
    public RecoveryBindingReconciliationFaultException(string code, RecoveryBindingReconciliationFaultPoint point)
        : base($"Controlled reconciliation persistence fault at {point}: {code}")
    {
        Code = code;
        Point = point;
    }

    public string Code { get; }
    public RecoveryBindingReconciliationFaultPoint Point { get; }
}

/// <summary>
/// Minimal fault-capable persistence model used only by the deterministic reconciliation fault harness.
/// It deliberately does not execute research or mutate production storage.
/// </summary>
internal sealed class RecoveryBindingReconciliationFaultStore
{
    private readonly RecoveryBindingReconciliationFaultPoint? _faultPoint;
    private bool _audit;
    private bool _evidence;
    private bool _event;
    private string? _bindingFingerprint;

    public RecoveryBindingReconciliationFaultStore(RecoveryBindingReconciliationFaultPoint? faultPoint = null)
        => _faultPoint = faultPoint;

    public void WriteAudit(string fingerprint)
    {
        ThrowIf(RecoveryBindingReconciliationFaultPoint.AuditWrite);
        if (_bindingFingerprint is not null && _bindingFingerprint != fingerprint) throw new InvalidOperationException("RECOVERY_BINDING_RECONCILIATION_CONFLICT");
        _bindingFingerprint = fingerprint;
        _audit = true;
    }

    public void WriteEvidence(string fingerprint)
    {
        ThrowIf(RecoveryBindingReconciliationFaultPoint.EvidenceWrite);
        if (_bindingFingerprint is not null && _bindingFingerprint != fingerprint) throw new InvalidOperationException("RECOVERY_BINDING_RECONCILIATION_CONFLICT");
        _bindingFingerprint ??= fingerprint;
        _evidence = true;
    }

    public void WriteEvent(string fingerprint)
    {
        ThrowIf(RecoveryBindingReconciliationFaultPoint.EventWrite);
        if (_bindingFingerprint is not null && _bindingFingerprint != fingerprint) throw new InvalidOperationException("RECOVERY_BINDING_RECONCILIATION_CONFLICT");
        _bindingFingerprint ??= fingerprint;
        _event = true;
    }

    public bool IsComplete(string fingerprint) => _audit && _evidence && _event && _bindingFingerprint == fingerprint;
    public bool HasAnyRecord => _audit || _evidence || _event;
    public void ClearFault() { }

    private void ThrowIf(RecoveryBindingReconciliationFaultPoint point)
    {
        if (_faultPoint == point) throw new RecoveryBindingReconciliationFaultException("QF_RECOVERY_RECONCILIATION_PERSISTENCE_FAULT", point);
    }
}
