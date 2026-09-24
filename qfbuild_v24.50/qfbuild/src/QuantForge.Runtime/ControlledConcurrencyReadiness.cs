using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record ConcurrencyReadinessIssue(string Code, string Detail);

public sealed record ControlledConcurrencyReadinessResult(
    string BatchId,
    string ManifestFingerprint,
    bool ReadyForDesignReview,
    bool ParallelExecutionEnabled,
    IReadOnlyList<ConcurrencyReadinessIssue> Issues,
    string ReviewFingerprint);

/// <summary>
/// Read-only gate documenting the invariants required before controlled parallel case execution.
/// v20.36 deliberately keeps ParallelExecutionEnabled=false.
/// </summary>
public sealed class ControlledConcurrencyReadinessService
{
    private readonly IResearchJobStore _jobs;
    private readonly IResourceReceiptStore _receipts;
    private readonly ILocalEvidenceStore? _evidence;

    public ControlledConcurrencyReadinessService(IResearchJobStore jobs, IResourceReceiptStore receipts, ILocalEvidenceStore? evidence = null)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        _evidence = evidence;
    }

    public async Task<ControlledConcurrencyReadinessResult> ReviewAsync(ResearchBatchManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var issues = new List<ConcurrencyReadinessIssue>();
        if (manifest.Cases.Count == 0) issues.Add(new("EMPTY_BATCH", "Concurrency review requires at least one declared case."));
        var contextIds = new HashSet<string>(StringComparer.Ordinal);
        var jobIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in manifest.Cases)
        {
            if (!contextIds.Add(c.ContextId)) issues.Add(new("DUPLICATE_CONTEXT", $"Context '{c.ContextId}' is duplicated."));
            if (!jobIds.Add(c.JobId)) issues.Add(new("DUPLICATE_JOB", $"Job '{c.JobId}' is duplicated."));
            var job = await _jobs.LoadJobAsync(c.JobId, cancellationToken);
            if (job is not null && (!string.Equals(job.DatasetFingerprint, c.DatasetFingerprint, StringComparison.Ordinal) ||
                                    !string.Equals(job.ConfigurationFingerprint, c.ConfigurationFingerprint, StringComparison.Ordinal) ||
                                    !string.Equals(job.EngineFingerprint, c.EngineFingerprint, StringComparison.Ordinal)))
                issues.Add(new("INPUT_FINGERPRINT_MISMATCH", $"Job '{c.JobId}' does not match its manifest identity."));
        }

        var receipts = await _receipts.LoadExecutionReceiptsForBatchAsync(manifest.BatchId, cancellationToken);
        if (receipts.Any(r => string.IsNullOrWhiteSpace(r.WorkerFingerprint)))
            issues.Add(new("WORKER_IDENTITY_INCOMPLETE", "Every execution receipt must identify its worker."));
        if (receipts.GroupBy(r => r.ContextId, StringComparer.Ordinal).Any(g => g.Count(x => IsTerminal(x.State)) > 1))
            issues.Add(new("TERMINAL_OUTCOME_DUPLICATION", "A case has multiple terminal execution receipts."));

        // Design gate only: actual parallel execution remains disabled until native resource/runtime validation.
        issues.Add(new("NATIVE_RUNTIME_VALIDATION_REQUIRED", "Parallel execution remains disabled until Windows/Android native runtime and resource behavior are validated."));
        var reviewFingerprint = ResearchFingerprint.Sha256($"{manifest.BatchId}|{manifest.ManifestFingerprint}|{string.Join("||", issues.OrderBy(x => x.Code, StringComparer.Ordinal).Select(x => $"{x.Code}|{x.Detail}"))}|PARALLEL_DISABLED");
        var result = new ControlledConcurrencyReadinessResult(manifest.BatchId, manifest.ManifestFingerprint, issues.All(x => x.Code != "DUPLICATE_CONTEXT" && x.Code != "DUPLICATE_JOB" && x.Code != "INPUT_FINGERPRINT_MISMATCH"), false, issues.AsReadOnly(), reviewFingerprint);
        if (_evidence is not null)
        {
            await _evidence.AppendEvidenceAsync($"controlled-concurrency-review:{reviewFingerprint}", reviewFingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:controlled-concurrency-review:{reviewFingerprint}", manifest.BatchId, "CONTROLLED_CONCURRENCY_DESIGN_REVIEWED", reviewFingerprint, cancellationToken);
        }
        return result;
    }

    private static bool IsTerminal(string state) =>
        string.Equals(state, nameof(MultiCaseItemState.Completed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Failed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Canceled), StringComparison.Ordinal);
}
