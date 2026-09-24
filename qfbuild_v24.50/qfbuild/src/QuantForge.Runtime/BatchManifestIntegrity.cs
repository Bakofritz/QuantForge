using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum BatchManifestIntegrityState
{
    Valid,
    InvalidManifest,
    ReceiptMismatch,
    JobStateMismatch,
    DuplicateTerminalOutcome
}

public sealed record BatchManifestIntegrityIssue(string Code, string ContextId, string JobId, string Detail);

public sealed record BatchManifestIntegrityResult(
    string BatchId,
    string ManifestFingerprint,
    BatchManifestIntegrityState State,
    IReadOnlyList<BatchManifestIntegrityIssue> Issues,
    string IntegrityFingerprint);

/// <summary>
/// Verifies that durable case receipts and job states agree with the immutable batch manifest.
/// It never changes a job or receipt and therefore cannot grant execution authority.
/// </summary>
public sealed class BatchManifestIntegrityService
{
    private readonly IResourceReceiptStore _receipts;
    private readonly IResearchJobStore _jobs;
    private readonly ILocalEvidenceStore? _evidence;

    public BatchManifestIntegrityService(IResourceReceiptStore receipts, IResearchJobStore jobs, ILocalEvidenceStore? evidence = null)
    {
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _evidence = evidence;
    }

    public async Task<BatchManifestIntegrityResult> ValidateAsync(ResearchBatchManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var issues = new List<BatchManifestIntegrityIssue>();
        var cases = manifest.Cases ?? Array.Empty<ResearchBatchManifestCase>();
        var contexts = new HashSet<string>(StringComparer.Ordinal);
        var jobs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var c in cases)
        {
            if (!contexts.Add(c.ContextId)) issues.Add(new("DUPLICATE_CONTEXT_ID", c.ContextId, c.JobId, "Manifest contains the same ContextId more than once."));
            if (!jobs.Add(c.JobId)) issues.Add(new("DUPLICATE_JOB_ID", c.ContextId, c.JobId, "Manifest contains the same JobId more than once."));
            if (string.IsNullOrWhiteSpace(c.DatasetFingerprint) || string.IsNullOrWhiteSpace(c.StrategyFingerprint) || string.IsNullOrWhiteSpace(c.ConfigurationFingerprint) || string.IsNullOrWhiteSpace(c.EngineFingerprint))
                issues.Add(new("INCOMPLETE_CASE_IDENTITY", c.ContextId, c.JobId, "Dataset, strategy, configuration, and engine fingerprints are required."));
        }

        var receipts = await _receipts.LoadExecutionReceiptsForBatchAsync(manifest.BatchId, cancellationToken);
        var manifestByContext = cases.ToDictionary(x => x.ContextId, StringComparer.Ordinal);

        foreach (var receipt in receipts)
        {
            if (!manifestByContext.TryGetValue(receipt.ContextId, out var expected))
            {
                issues.Add(new("RECEIPT_CONTEXT_NOT_IN_MANIFEST", receipt.ContextId, receipt.JobId, "Receipt references a context not declared by the batch manifest."));
                continue;
            }
            if (!string.Equals(receipt.JobId, expected.JobId, StringComparison.Ordinal))
                issues.Add(new("RECEIPT_JOB_MISMATCH", receipt.ContextId, receipt.JobId, $"Manifest expects JobId '{expected.JobId}'."));

            var terminal = receipts.Where(x => string.Equals(x.ContextId, receipt.ContextId, StringComparison.Ordinal)
                                               && IsTerminal(x.State)).ToArray();
            if (terminal.Select(x => x.ReceiptFingerprint).Distinct(StringComparer.Ordinal).Count() > 1)
                issues.Add(new("MULTIPLE_TERMINAL_RECEIPTS", receipt.ContextId, expected.JobId, "More than one distinct terminal receipt exists for the case history; reconciliation is required before terminal agreement."));
        }

        foreach (var c in cases)
        {
            var job = await _jobs.LoadJobAsync(c.JobId, cancellationToken);
            if (job is null) continue;
            if (!string.Equals(job.DatasetFingerprint, c.DatasetFingerprint, StringComparison.Ordinal))
                issues.Add(new("JOB_DATASET_FINGERPRINT_MISMATCH", c.ContextId, c.JobId, "Durable job dataset fingerprint differs from the manifest."));
            if (!string.Equals(job.ConfigurationFingerprint, c.ConfigurationFingerprint, StringComparison.Ordinal))
                issues.Add(new("JOB_CONFIGURATION_FINGERPRINT_MISMATCH", c.ContextId, c.JobId, "Durable job configuration fingerprint differs from the manifest."));
            if (!string.Equals(job.EngineFingerprint, c.EngineFingerprint, StringComparison.Ordinal))
                issues.Add(new("JOB_ENGINE_FINGERPRINT_MISMATCH", c.ContextId, c.JobId, "Durable job engine fingerprint differs from the manifest."));

            var terminalReceipts = receipts.Where(x => string.Equals(x.ContextId, c.ContextId, StringComparison.Ordinal) && IsTerminal(x.State)).ToArray();
            if (job.State == DurableJobState.Completed && terminalReceipts.Length == 0)
                issues.Add(new("COMPLETED_JOB_WITHOUT_TERMINAL_RECEIPT", c.ContextId, c.JobId, "Job is Completed but no terminal execution receipt exists."));
            if (job.State == DurableJobState.Canceled && terminalReceipts.All(x => !string.Equals(x.State, nameof(MultiCaseItemState.Canceled), StringComparison.Ordinal)))
                issues.Add(new("CANCELED_JOB_RECEIPT_MISMATCH", c.ContextId, c.JobId, "Job is Canceled but no canceled terminal receipt exists."));
            if (job.State == DurableJobState.Failed && terminalReceipts.All(x => !string.Equals(x.State, nameof(MultiCaseItemState.Failed), StringComparison.Ordinal)))
                issues.Add(new("FAILED_JOB_RECEIPT_MISMATCH", c.ContextId, c.JobId, "Job is Failed but no failed terminal receipt exists."));
        }

        var state = issues.Count == 0 ? BatchManifestIntegrityState.Valid
            : issues.Any(x => x.Code == "MULTIPLE_TERMINAL_RECEIPTS") ? BatchManifestIntegrityState.DuplicateTerminalOutcome
            : issues.Any(x => x.Code.StartsWith("RECEIPT_", StringComparison.Ordinal)) ? BatchManifestIntegrityState.ReceiptMismatch
            : issues.Any(x => x.Code.StartsWith("JOB_", StringComparison.Ordinal) || x.Code.EndsWith("RECEIPT", StringComparison.Ordinal)) ? BatchManifestIntegrityState.JobStateMismatch
            : BatchManifestIntegrityState.InvalidManifest;

        var issueFingerprint = ResearchFingerprint.Sha256(string.Join("||", issues.OrderBy(x => x.ContextId, StringComparer.Ordinal).ThenBy(x => x.Code, StringComparer.Ordinal).Select(x => $"{x.Code}|{x.ContextId}|{x.JobId}|{x.Detail}")));
        var integrityFingerprint = ResearchFingerprint.Sha256($"{manifest.ManifestFingerprint}|{state}|{issueFingerprint}");
        var result = new BatchManifestIntegrityResult(manifest.BatchId, manifest.ManifestFingerprint, state, issues.AsReadOnly(), integrityFingerprint);

        if (_evidence is not null)
        {
            await _evidence.AppendEvidenceAsync($"batch-manifest-integrity:{integrityFingerprint}", integrityFingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:batch-manifest-integrity:{integrityFingerprint}", manifest.BatchId, "BATCH_MANIFEST_INTEGRITY_VALIDATED", integrityFingerprint, cancellationToken);
        }
        return result;
    }

    private static bool IsTerminal(string state) =>
        string.Equals(state, nameof(MultiCaseItemState.Completed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Failed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Canceled), StringComparison.Ordinal);
}
