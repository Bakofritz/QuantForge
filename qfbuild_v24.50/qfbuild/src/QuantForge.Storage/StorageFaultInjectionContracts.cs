namespace QuantForge.Storage;

public enum StorageFaultPoint
{
    None,
    LeaseAcquire,
    LeaseRenew,
    CheckpointWrite,
    ReceiptWrite,
    EvidenceWrite,
    ValidationResultWrite,
    JobStateWrite,
    RecoveryAuditWrite,
    TerminalEventWrite,
    TerminalCommitBeforeCommit
}

public enum StorageFaultDisposition
{
    NotInjected,
    Injected,
    ObservedAndContained,
    UnexpectedlySucceeded,
    UnexpectedException
}

public sealed record StorageFaultPlan(
    StorageFaultPoint Point,
    int FailOnInvocation = 1,
    string ErrorCode = "QF_STORAGE_FAULT_INJECTED")
{
    public void Validate()
    {
        if (FailOnInvocation < 1) throw new ArgumentOutOfRangeException(nameof(FailOnInvocation));
        if (Point == StorageFaultPoint.None) throw new ArgumentException("Fault point must be explicit.", nameof(Point));
        if (string.IsNullOrWhiteSpace(ErrorCode)) throw new ArgumentException("Error code is required.", nameof(ErrorCode));
    }
}

public sealed class StorageFaultInjector
{
    private readonly StorageFaultPlan? _plan;
    private int _invocations;

    public StorageFaultInjector(StorageFaultPlan? plan = null)
    {
        plan?.Validate();
        _plan = plan;
    }

    public bool IsConfigured => _plan is not null;
    public StorageFaultPoint Point => _plan?.Point ?? StorageFaultPoint.None;
    public int InvocationCount => _invocations;

    public void ThrowIfConfigured(StorageFaultPoint point)
    {
        var plan = _plan;
        if (plan is null || plan.Point != point) return;
        var invocation = Interlocked.Increment(ref _invocations);
        if (invocation == plan.FailOnInvocation)
            throw new StorageFaultInjectedException(plan.ErrorCode, point, invocation);
    }
}

public sealed class StorageFaultInjectedException : Exception
{
    public StorageFaultInjectedException(string errorCode, StorageFaultPoint point, int invocation)
        : base($"Controlled storage fault injected at {point} on invocation {invocation}: {errorCode}")
    {
        ErrorCode = errorCode;
        Point = point;
        Invocation = invocation;
    }

    public string ErrorCode { get; }
    public StorageFaultPoint Point { get; }
    public int Invocation { get; }
}

public sealed record StorageFaultInjectionCheck(
    StorageFaultPoint Point,
    StorageFaultDisposition Disposition,
    string Detail);

public sealed record StorageFaultInjectionResult(
    string HarnessVersion,
    bool Passed,
    IReadOnlyList<StorageFaultInjectionCheck> Checks,
    string ResultFingerprint,
    bool FailClosedExpected,
    bool NativeStorageExecuted);
