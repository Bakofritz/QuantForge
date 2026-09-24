namespace QuantForge.Storage;

public enum TransactionalTerminalFaultPoint
{
    JobStateWrite,
    ReceiptWrite,
    EvidenceWrite,
    EventWrite,
    BeforeCommit
}

public sealed record TransactionalTerminalFaultCheck(
    TransactionalTerminalFaultPoint Point,
    bool FaultContained,
    bool RolledBack,
    bool RetryCommitted,
    bool TerminalStateVerified,
    bool ReceiptVerified,
    bool EvidenceVerified,
    bool EventVerified,
    string Detail);

public sealed record TransactionalTerminalFaultValidationResult(
    string HarnessVersion,
    bool Passed,
    IReadOnlyList<TransactionalTerminalFaultCheck> Checks,
    string ResultFingerprint,
    bool FailClosedExpected,
    bool NativeStorageExecuted);
