using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum TerminalStateAgreement { Agree, Disagree, Incomplete }

public sealed record TerminalStateAgreementIssue(string Code, string ContextId, string JobId, string Detail);

public sealed record TerminalStateAgreementResult(
    string BatchId,
    string ManifestFingerprint,
    TerminalStateAgreement State,
    IReadOnlyList<TerminalStateAgreementIssue> Issues,
    string AgreementFingerprint);

/// <summary>
/// Read-only terminal-state audit. It compares durable jobs and execution receipts without
/// changing either record or granting execution authority.
/// </summary>
public sealed class TerminalStateAgreementService
{
    private readonly IResearchJobStore _jobs;
    private readonly IResourceReceiptStore _receipts;
    private readonly ILocalEvidenceStore? _evidence;

    public TerminalStateAgreementService(IResearchJobStore jobs, IResourceReceiptStore receipts, ILocalEvidenceStore? evidence = null)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        _evidence = evidence;
    }

    public async Task<TerminalStateAgreementResult> ValidateAsync(ResearchBatchManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var issues = new List<TerminalStateAgreementIssue>();
        var receipts = await _receipts.LoadExecutionReceiptsForBatchAsync(manifest.BatchId, cancellationToken);

        foreach (var c in manifest.Cases)
        {
            var job = await _jobs.LoadJobAsync(c.JobId, cancellationToken);
            var terminal = receipts.Where(r => string.Equals(r.ContextId, c.ContextId, StringComparison.Ordinal) && IsTerminal(r.State)).ToArray();

            if (job is null)
            {
                issues.Add(new("JOB_NOT_FOUND", c.ContextId, c.JobId, "Manifest case has no durable job record."));
                continue;
            }

            if (terminal.Length > 1)
                issues.Add(new("MULTIPLE_TERMINAL_RECEIPTS", c.ContextId, c.JobId, "More than one terminal receipt exists for the case."));

            var receipt = terminal.SingleOrDefault();
            if (job.State == DurableJobState.Completed)
            {
                if (receipt is null || !string.Equals(receipt.State, nameof(MultiCaseItemState.Completed), StringComparison.Ordinal))
                    issues.Add(new("COMPLETED_STATE_DISAGREEMENT", c.ContextId, c.JobId, "Completed job does not have exactly one Completed terminal receipt."));
                else if (string.IsNullOrWhiteSpace(receipt.ResultFingerprint))
                    issues.Add(new("COMPLETED_RECEIPT_MISSING_RESULT", c.ContextId, c.JobId, "Completed receipt has no result fingerprint."));
            }
            else if (job.State == DurableJobState.Failed)
            {
                if (receipt is null || !string.Equals(receipt.State, nameof(MultiCaseItemState.Failed), StringComparison.Ordinal))
                    issues.Add(new("FAILED_STATE_DISAGREEMENT", c.ContextId, c.JobId, "Failed job does not have exactly one Failed terminal receipt."));
            }
            else if (job.State == DurableJobState.Canceled)
            {
                if (receipt is null || !string.Equals(receipt.State, nameof(MultiCaseItemState.Canceled), StringComparison.Ordinal))
                    issues.Add(new("CANCELED_STATE_DISAGREEMENT", c.ContextId, c.JobId, "Canceled job does not have exactly one Canceled terminal receipt."));
            }
            else if (receipt is not null)
            {
                issues.Add(new("TERMINAL_RECEIPT_WITH_NONTERMINAL_JOB", c.ContextId, c.JobId, $"Job state is {job.State} but a terminal receipt exists."));
            }
        }

        var state = issues.Count == 0
            ? TerminalStateAgreement.Agree
            : issues.Any(x => x.Code == "JOB_NOT_FOUND") ? TerminalStateAgreement.Incomplete
            : TerminalStateAgreement.Disagree;
        var issueFingerprint = ResearchFingerprint.Sha256(string.Join("||", issues.OrderBy(x => x.ContextId, StringComparer.Ordinal).ThenBy(x => x.Code, StringComparer.Ordinal).Select(x => $"{x.Code}|{x.ContextId}|{x.JobId}|{x.Detail}")));
        var fingerprint = ResearchFingerprint.Sha256($"{manifest.BatchId}|{manifest.ManifestFingerprint}|{state}|{issueFingerprint}");
        var result = new TerminalStateAgreementResult(manifest.BatchId, manifest.ManifestFingerprint, state, issues.AsReadOnly(), fingerprint);

        if (_evidence is not null)
        {
            await _evidence.AppendEvidenceAsync($"terminal-state-agreement:{fingerprint}", fingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:terminal-state-agreement:{fingerprint}", manifest.BatchId, "TERMINAL_STATE_AGREEMENT_VALIDATED", fingerprint, cancellationToken);
        }
        return result;
    }

    private static bool IsTerminal(string state) =>
        string.Equals(state, nameof(MultiCaseItemState.Completed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Failed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Canceled), StringComparison.Ordinal);
}
