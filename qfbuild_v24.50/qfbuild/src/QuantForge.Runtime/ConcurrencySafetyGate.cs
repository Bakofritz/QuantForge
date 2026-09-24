using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record ConcurrencyResourceBudget(
    long MaxTotalElapsedMilliseconds,
    int MaxTotalHeartbeatRenewals,
    int MaxConcurrentCases,
    long MaxEstimatedMemoryMegabytes)
{
    public void Validate()
    {
        if (MaxTotalElapsedMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(MaxTotalElapsedMilliseconds));
        if (MaxTotalHeartbeatRenewals < 0) throw new ArgumentOutOfRangeException(nameof(MaxTotalHeartbeatRenewals));
        if (MaxConcurrentCases < 1) throw new ArgumentOutOfRangeException(nameof(MaxConcurrentCases));
        if (MaxEstimatedMemoryMegabytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxEstimatedMemoryMegabytes));
    }
}

public sealed record ConcurrencyContentionDomain(string Name, string ResourceKind, bool SharedWriter, string Policy)
{
    public static IReadOnlyList<ConcurrencyContentionDomain> DefaultDomains => new ConcurrencyContentionDomain[]
    {
        new("SQLite", "LOCAL_DATABASE", true, "SERIALIZE_WRITES"),
        new("EvidenceStore", "IMMUTABLE_EVIDENCE", true, "IDEMPOTENT_APPEND"),
        new("LeaseStore", "JOB_LEASE", true, "LEASE_SERIALIZATION"),
        new("BatchAccounting", "BATCH_ACCOUNTING", true, "DERIVE_FROM_TERMINAL_RECEIPTS"),
        new("CheckpointStore", "CHECKPOINT", true, "FINGERPRINT_BOUND_APPEND"),
        new("CaseState", "CASE_MUTABLE_STATE", false, "ISOLATE_PER_CASE")
    };
}

public sealed record ConcurrencySafetyGateResult(
    string BatchId,
    string ManifestFingerprint,
    bool ArchitectureSafe,
    bool ParallelExecutionEnabled,
    IReadOnlyList<ConcurrencyReadinessIssue> Issues,
    string ResourcePolicyFingerprint,
    string GateFingerprint);

/// <summary>
/// v20.37 formalizes shared-resource contention boundaries without enabling parallel execution.
/// It is an architecture/resource gate, not proof of native runtime safety.
/// </summary>
public sealed class ConcurrencySafetyGateService
{
    private readonly IResearchJobStore _jobs;
    private readonly IResourceReceiptStore _receipts;
    private readonly ILocalEvidenceStore? _evidence;

    public ConcurrencySafetyGateService(IResearchJobStore jobs, IResourceReceiptStore receipts, ILocalEvidenceStore? evidence = null)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        _evidence = evidence;
    }

    public async Task<ConcurrencySafetyGateResult> EvaluateAsync(
        ResearchBatchManifest manifest,
        ConcurrencyResourceBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        budget.Validate();
        var issues = new List<ConcurrencyReadinessIssue>();
        var domains = ConcurrencyContentionDomain.DefaultDomains;

        if (budget.MaxConcurrentCases > 1)
            issues.Add(new("NATIVE_PARALLEL_EXECUTION_DISABLED", "Requested concurrency exceeds the validated sequential limit."));
        if (domains.Any(d => d.SharedWriter && string.IsNullOrWhiteSpace(d.Policy)))
            issues.Add(new("SHARED_RESOURCE_POLICY_MISSING", "Every shared writer resource requires an explicit contention policy."));

        var contextIds = new HashSet<string>(StringComparer.Ordinal);
        var jobIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in manifest.Cases)
        {
            if (!contextIds.Add(c.ContextId)) issues.Add(new("DUPLICATE_CONTEXT", $"Context '{c.ContextId}' is duplicated."));
            if (!jobIds.Add(c.JobId)) issues.Add(new("DUPLICATE_JOB", $"Job '{c.JobId}' is duplicated."));
            var job = await _jobs.LoadJobAsync(c.JobId, cancellationToken);
            if (job is not null &&
                (!string.Equals(job.DatasetFingerprint, c.DatasetFingerprint, StringComparison.Ordinal) ||
                 !string.Equals(job.ConfigurationFingerprint, c.ConfigurationFingerprint, StringComparison.Ordinal) ||
                 !string.Equals(job.EngineFingerprint, c.EngineFingerprint, StringComparison.Ordinal)))
                issues.Add(new("INPUT_FINGERPRINT_MISMATCH", $"Job '{c.JobId}' does not match its manifest identity."));
        }

        var receipts = await _receipts.LoadExecutionReceiptsForBatchAsync(manifest.BatchId, cancellationToken);
        var terminalReceipts = receipts.Where(r => IsTerminal(r.State)).ToArray();
        if (terminalReceipts.GroupBy(r => r.ContextId, StringComparer.Ordinal).Any(g => g.Count() > 1))
            issues.Add(new("TERMINAL_OUTCOME_RACE", "A case has more than one terminal receipt; parallel execution cannot proceed until reconciled."));
        if (receipts.Any(r => string.IsNullOrWhiteSpace(r.WorkerFingerprint)))
            issues.Add(new("WORKER_IDENTITY_INCOMPLETE", "Every execution receipt must identify its worker."));

        var resourceFingerprint = ResearchFingerprint.Sha256(string.Join("|", new[]
        {
            budget.MaxTotalElapsedMilliseconds.ToString(), budget.MaxTotalHeartbeatRenewals.ToString(),
            budget.MaxConcurrentCases.ToString(), budget.MaxEstimatedMemoryMegabytes.ToString(),
            string.Join("||", domains.Select(d => $"{d.Name}:{d.ResourceKind}:{d.SharedWriter}:{d.Policy}"))
        }));
        var gateFingerprint = ResearchFingerprint.Sha256($"{manifest.BatchId}|{manifest.ManifestFingerprint}|{resourceFingerprint}|{string.Join("||", issues.OrderBy(x => x.Code, StringComparer.Ordinal).Select(x => $"{x.Code}|{x.Detail}"))}|PARALLEL_DISABLED");
        var safe = issues.All(x => x.Code is not "DUPLICATE_CONTEXT" and not "DUPLICATE_JOB" and not "INPUT_FINGERPRINT_MISMATCH" and not "TERMINAL_OUTCOME_RACE" and not "WORKER_IDENTITY_INCOMPLETE") && budget.MaxConcurrentCases == 1;
        var result = new ConcurrencySafetyGateResult(manifest.BatchId, manifest.ManifestFingerprint, safe, false, issues.AsReadOnly(), resourceFingerprint, gateFingerprint);
        if (_evidence is not null)
        {
            await _evidence.AppendEvidenceAsync($"concurrency-safety-gate:{gateFingerprint}", gateFingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:concurrency-safety-gate:{gateFingerprint}", manifest.BatchId, "CONCURRENCY_SAFETY_GATE_EVALUATED", gateFingerprint, cancellationToken);
        }
        return result;
    }

    private static bool IsTerminal(string state) =>
        string.Equals(state, nameof(MultiCaseItemState.Completed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Failed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(MultiCaseItemState.Canceled), StringComparison.Ordinal);
}
