using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum RecoveryIntegrityIssueCode
{
    FailedRecoveryMarkedCompleted,
    TerminalReceiptWithoutTerminalJob,
    JobMutationWithoutValidLease,
    CheckpointAfterPersistenceFailure,
    DuplicateTerminalOutcome,
    RecoveryAfterCancellation,
    LeaseOwnershipChanged,
    SuccessfulResultWithoutDurableTerminalState
}

public sealed record RecoveryIntegrityIssue(
    RecoveryIntegrityIssueCode Code,
    string JobId,
    string Detail);

public sealed record RecoveryStateIntegrityResult(
    string JobId,
    bool Passed,
    IReadOnlyList<RecoveryIntegrityIssue> Issues,
    string ResultFingerprint,
    bool FailClosedExpected);

/// <summary>
/// Read-only invariant validator for recovery/terminal state. It does not invent progress,
/// repair durable state, or resume jobs. Any ambiguity is treated as a failure condition.
/// </summary>
public sealed class RecoveryStateIntegrityService
{
    private readonly IResearchJobStore _jobs;
    private readonly ILeaseInspectionStore _leases;
    private readonly IResourceReceiptStore _receipts;

    public RecoveryStateIntegrityService(IResearchJobStore jobs, ILeaseInspectionStore leases, IResourceReceiptStore receipts)
    {
        _jobs = jobs;
        _leases = leases;
        _receipts = receipts;
    }

    public async Task<RecoveryStateIntegrityResult> ValidateAsync(
        string jobId,
        string expectedWorkerDeviceId,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<RecoveryIntegrityIssue>();
        var job = await _jobs.LoadJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            issues.Add(new(RecoveryIntegrityIssueCode.SuccessfulResultWithoutDurableTerminalState, jobId, "Job record is missing."));
            return Create(jobId, issues);
        }

        var receipts = await _receipts.LoadExecutionReceiptsForJobAsync(jobId, cancellationToken);
        var terminalReceipts = receipts.Where(IsTerminal).ToArray();
        var lease = await _leases.LoadLeaseAsync(jobId, cancellationToken);
        var cancellation = await _jobs.LoadCancellationAsync(jobId, cancellationToken);

        if (terminalReceipts.Length > 1)
            issues.Add(new(RecoveryIntegrityIssueCode.DuplicateTerminalOutcome, jobId, "More than one terminal execution receipt exists."));

        if (terminalReceipts.Length == 1 && !IsTerminalState(job.State))
            issues.Add(new(RecoveryIntegrityIssueCode.TerminalReceiptWithoutTerminalJob, jobId, $"Terminal receipt state '{terminalReceipts[0].State}' disagrees with durable job state '{job.State}'."));

        if (job.State == DurableJobState.Completed && terminalReceipts.Length != 1)
            issues.Add(new(RecoveryIntegrityIssueCode.SuccessfulResultWithoutDurableTerminalState, jobId, "Completed job requires exactly one terminal execution receipt."));

        if (job.State == DurableJobState.Completed && terminalReceipts.Length == 1 && terminalReceipts[0].State != nameof(MultiCaseItemState.Completed))
            issues.Add(new(RecoveryIntegrityIssueCode.FailedRecoveryMarkedCompleted, jobId, "Completed job has a non-completed terminal receipt."));

        if (cancellation is not null && terminalReceipts.Any(r => r.State == nameof(MultiCaseItemState.Completed)))
            issues.Add(new(RecoveryIntegrityIssueCode.RecoveryAfterCancellation, jobId, "A durable cancellation request coexists with a completed terminal outcome."));

        if (job.State == DurableJobState.RecoveryRequired && cancellation is not null)
            issues.Add(new(RecoveryIntegrityIssueCode.RecoveryAfterCancellation, jobId, "RecoveryRequired job has a durable cancellation request."));

        if (lease is not null && lease.ExpiresAt <= DateTimeOffset.UtcNow && job.State == DurableJobState.Running)
            issues.Add(new(RecoveryIntegrityIssueCode.JobMutationWithoutValidLease, jobId, "Running job has an expired lease."));

        if (lease is not null && !string.Equals(lease.DeviceId, expectedWorkerDeviceId, StringComparison.Ordinal) && lease.ExpiresAt > DateTimeOffset.UtcNow)
            issues.Add(new(RecoveryIntegrityIssueCode.LeaseOwnershipChanged, jobId, "Active lease is owned by a different device than the expected worker."));

        return Create(jobId, issues);
    }

    private static bool IsTerminalState(DurableJobState state) =>
        state is DurableJobState.Completed or DurableJobState.Failed or DurableJobState.Canceled;

    private static bool IsTerminal(ResourceReceiptRecord receipt) =>
        receipt.State is nameof(MultiCaseItemState.Completed) or nameof(MultiCaseItemState.Failed) or nameof(MultiCaseItemState.Canceled);

    private static RecoveryStateIntegrityResult Create(string jobId, List<RecoveryIntegrityIssue> issues)
    {
        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", issues.Select(i => $"{i.Code}|{i.JobId}|{i.Detail}")));
        return new RecoveryStateIntegrityResult(jobId, issues.Count == 0, issues.AsReadOnly(), fingerprint, true);
    }
}
