namespace QuantForge.Storage;

public sealed record StorageFaultRecoveryCheck(
    StorageFaultPoint Point,
    bool FaultContained,
    bool RetrySucceeded,
    bool DurableStateVerified,
    string Detail);

public sealed record StorageFaultRecoveryValidationResult(
    string HarnessVersion,
    bool Passed,
    IReadOnlyList<StorageFaultRecoveryCheck> Checks,
    string ResultFingerprint,
    bool FailClosedExpected,
    bool NativeStorageExecuted);
