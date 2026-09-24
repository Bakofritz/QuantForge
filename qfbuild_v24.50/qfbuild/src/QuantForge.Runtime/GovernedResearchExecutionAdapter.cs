using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record GovernedResearchExecutionRequest(
    MultiCaseExecutionRequest Batch,
    MultiCaseResourcePolicy ResourcePolicy,
    long EstimatedMemoryBytesPerCase,
    long MemoryBudgetBytes,
    bool NativeRuntimeQualified,
    bool ReadOnlyResearchAuthority = true);

public sealed record GovernedResearchExecutionResult(
    bool Admitted,
    string? StopCode,
    MultiCaseExecutionResult? Execution,
    string AdmissionFingerprint,
    string ResultFingerprint);

/// <summary>
/// First governed execution adapter. It composes native qualification, resource admission,
/// and the existing lease-aware multi-case coordinator. It deliberately exposes only research
/// execution; live order, arbitrary-code, and application-mutation authorities are prohibited.
/// </summary>
public sealed class GovernedResearchExecutionAdapter
{
    private readonly ConcurrencyResourceGate _resourceGate;
    private readonly MultiCaseExecutionCoordinator _coordinator;

    public GovernedResearchExecutionAdapter(IJobLeaseStore leases, IResearchJobStore? jobs = null, ILocalEvidenceStore? evidence = null, IResourceReceiptStore? resourceStore = null)
    {
        _resourceGate = new ConcurrencyResourceGate();
        _coordinator = new MultiCaseExecutionCoordinator(leases, jobs, evidence, resourceStore);
    }

    public async Task<GovernedResearchExecutionResult> ExecuteAsync(
        GovernedResearchExecutionRequest request,
        Func<ResearchContextState, CancellationToken, Task<string>> executeCaseAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executeCaseAsync);
        request.ResourcePolicy.Validate();
        if (!request.ReadOnlyResearchAuthority)
            throw new InvalidOperationException("GOVERNED_RESEARCH_REQUIRES_READ_ONLY_AUTHORITY");

        var admission = _resourceGate.Evaluate(
            request.Batch.Cases.Count,
            request.ResourcePolicy.MaxConcurrentCases,
            request.EstimatedMemoryBytesPerCase,
            request.MemoryBudgetBytes,
            request.NativeRuntimeQualified);

        if (!admission.Allowed)
        {
            var denied = QuantForge.Core.ResearchFingerprint.Sha256($"DENIED|{request.Batch.BatchId}|{admission.Fingerprint}|{admission.StopCode}");
            return new(false, admission.StopCode, null, admission.Fingerprint, denied);
        }

        var execution = await _coordinator.ExecuteAsync(
            request.Batch,
            executeCaseAsync,
            request.ResourcePolicy,
            cancellationToken);
        var resultFingerprint = QuantForge.Core.ResearchFingerprint.Sha256(
            $"{request.Batch.BatchId}|{admission.Fingerprint}|{execution.AggregateFingerprint}|{execution.Canceled}");
        return new(true, null, execution, admission.Fingerprint, resultFingerprint);
    }
}

public sealed record GovernedResearchExecutionValidationResult(
    bool Passed,
    IReadOnlyList<string> Checks,
    string Fingerprint);

/// <summary>Deterministic architecture harness for the governed execution boundary.</summary>
public sealed class GovernedResearchExecutionValidationHarness
{
    public GovernedResearchExecutionValidationResult Run()
    {
        var checks = new List<string>();
        checks.Add(Check("UNQUALIFIED_RUNTIME_BLOCKED", !Evaluate(false, 1024, 1024).Allowed));
        checks.Add(Check("UNKNOWN_RESOURCE_BLOCKED", !Evaluate(true, 0, 1024).Allowed));
        checks.Add(Check("MEMORY_OVER_BUDGET_BLOCKED", !Evaluate(true, 2048, 1024).Allowed));
        checks.Add(Check("QUALIFIED_WITHIN_BUDGET_ADMITTED", Evaluate(true, 512, 1024).Allowed));
        checks.Add(Check("EFFECTIVE_CONCURRENCY_BOUNDED", Evaluate(true, 512, 1024).EffectiveConcurrency == 1));
        checks.Add(Check("LIVE_ORDER_AUTHORITY_PROHIBITED", QuantForgeAuthority.ProhibitedAuthorities.Contains("LIVE_ORDER")));
        checks.Add(Check("ARBITRARY_CODE_AUTHORITY_PROHIBITED", QuantForgeAuthority.ProhibitedAuthorities.Contains("ARBITRARY_CODE_EXECUTION")));
        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks));
        return new(checks.All(x => x.EndsWith(":PASS", StringComparison.Ordinal)), checks, fingerprint);
    }

    private static ConcurrencyResourceGateResult Evaluate(bool qualified, long estimate, long budget)
        => new ConcurrencyResourceGate().Evaluate(4, 1, estimate, budget, qualified);

    private static string Check(string name, bool passed) => $"{name}:{(passed ? "PASS" : "FAIL")}";
}
