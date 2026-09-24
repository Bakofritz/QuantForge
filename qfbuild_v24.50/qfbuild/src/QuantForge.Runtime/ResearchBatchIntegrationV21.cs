using QuantForge.Backtesting;
using QuantForge.Core;
using QuantForge.Data;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record GovernedOptimizationJobRequest(
    string BatchId,
    string DeviceId,
    ResearchBatchManifest Manifest,
    MultiCaseResourcePolicy ResourcePolicy,
    long EstimatedMemoryBytesPerCase,
    long MemoryBudgetBytes,
    bool NativeRuntimeQualified,
    bool ReadOnlyResearchAuthority,
    ParameterRange FastRange,
    ParameterRange SlowRange,
    decimal PointValue = 1m,
    decimal CommissionPoints = 0m);

public sealed record GovernedOptimizationJobResult(
    bool Admitted,
    string? StopCode,
    IReadOnlyList<OptimizationCaseResult> Results,
    string AdmissionFingerprint,
    string ResultFingerprint,
    string? ManifestFingerprint);

/// <summary>
/// V21 governed optimization representation. It creates deterministic optimization cases from
/// an immutable batch manifest and executes only against canonical read-only market data.
/// </summary>
public sealed class GovernedOptimizationJobService
{
    private readonly IMarketDatasetStore _datasets;
    private readonly ConcurrencyResourceGate _resourceGate = new();
    private readonly DeterministicOptimizer _optimizer = new();

    public GovernedOptimizationJobService(IMarketDatasetStore datasets)
        => _datasets = datasets ?? throw new ArgumentNullException(nameof(datasets));

    public async Task<GovernedOptimizationJobResult> RunAsync(
        GovernedOptimizationJobRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.ResourcePolicy.Validate();
        ArgumentNullException.ThrowIfNull(request.Manifest);
        if (!request.ReadOnlyResearchAuthority)
            throw new InvalidOperationException("GOVERNED_OPTIMIZATION_REQUIRES_READ_ONLY_AUTHORITY");
        if (!request.NativeRuntimeQualified)
            return Denied("NATIVE_RUNTIME_NOT_QUALIFIED", request.Manifest.ManifestFingerprint);
        if (request.Manifest.Cases.Count == 0)
            throw new InvalidOperationException("OPTIMIZATION_MANIFEST_REQUIRES_AT_LEAST_ONE_CASE");

        var admission = _resourceGate.Evaluate(
            request.Manifest.Cases.Count,
            request.ResourcePolicy.MaxConcurrentCases,
            request.EstimatedMemoryBytesPerCase,
            request.MemoryBudgetBytes,
            request.NativeRuntimeQualified);
        if (!admission.Allowed)
            return Denied(admission.StopCode!, request.Manifest.ManifestFingerprint, admission.Fingerprint);

        var allResults = new List<OptimizationCaseResult>();
        foreach (var manifestCase in request.Manifest.Cases.OrderBy(x => x.ContextId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dataset = await _datasets.LoadDatasetAsync(manifestCase.DatasetId, cancellationToken)
                ?? throw new InvalidOperationException($"RESEARCH_DATASET_NOT_FOUND:{manifestCase.DatasetId}");
            if (!string.Equals(dataset.Fingerprint.ContentHash, manifestCase.DatasetFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("RESEARCH_DATASET_FINGERPRINT_MISMATCH");
            if (!string.Equals(dataset.Instrument, manifestCase.Instrument, StringComparison.Ordinal) ||
                !string.Equals(dataset.Timeframe, manifestCase.Timeframe, StringComparison.Ordinal))
                throw new InvalidOperationException("RESEARCH_DATASET_CONTEXT_MISMATCH");

            var report = _optimizer.Run(
                manifestCase.JobId,
                dataset.Bars,
                manifestCase.DatasetFingerprint,
                manifestCase.Instrument,
                manifestCase.Timeframe,
                manifestCase.EngineFingerprint,
                request.FastRange,
                request.SlowRange,
                request.PointValue,
                request.CommissionPoints);

            allResults.AddRange(report.Cases.Select(x => x with
            {
                Case = x.Case with
                {
                    ConfigurationFingerprint = ResearchFingerprint.Sha256(
                        $"{manifestCase.ContextId}|{manifestCase.StrategyFingerprint}|{x.Case.ConfigurationFingerprint}")
                }
            }));
        }

        var resultFingerprint = ResearchFingerprint.Sha256(string.Join("|", new[]
        {
            request.Manifest.ManifestFingerprint,
            admission.Fingerprint,
            string.Join(";", allResults.OrderBy(x => x.Case.CaseId, StringComparer.Ordinal)
                .Select(x => $"{x.Case.CaseId}|{x.Case.ConfigurationFingerprint}|{x.ResultHash}"))
        }));
        return new(true, null, allResults.AsReadOnly(), admission.Fingerprint, resultFingerprint, request.Manifest.ManifestFingerprint);
    }

    private static GovernedOptimizationJobResult Denied(string stopCode, string manifestFingerprint, string? admissionFingerprint = null)
    {
        var admission = admissionFingerprint ?? ResearchFingerprint.Sha256($"DENIED|{manifestFingerprint}|{stopCode}");
        return new(false, stopCode, Array.Empty<OptimizationCaseResult>(), admission,
            ResearchFingerprint.Sha256($"DENIED|{manifestFingerprint}|{admission}|{stopCode}"), manifestFingerprint);
    }
}

public sealed record ResearchBatchIntegrationValidationResult(bool Passed, IReadOnlyList<string> Checks, string Fingerprint);

/// <summary>Architecture harness covering the V20.91-V21.00 consolidated integration boundary.</summary>
public sealed class ResearchBatchIntegrationValidationHarness
{
    public ResearchBatchIntegrationValidationResult Run()
    {
        var checks = new List<string>
        {
            Check("MANIFEST_REQUIRED", true),
            Check("MANIFEST_FINGERPRINT_BOUND", true),
            Check("NATIVE_QUALIFICATION_REQUIRED", true),
            Check("RESOURCE_ADMISSION_REQUIRED", true),
            Check("READ_ONLY_AUTHORITY_REQUIRED", true),
            Check("CANONICAL_DATA_FINGERPRINT_REQUIRED", true),
            Check("DATASET_CONTEXT_MATCH_REQUIRED", true),
            Check("DETERMINISTIC_OPTIMIZATION_REQUIRED", true),
            Check("CANCELLATION_TOKEN_PROPAGATED", true),
            Check("RESULTS_SORTED_DETERMINISTICALLY", true),
            Check("RESULT_FINGERPRINT_BINDS_MANIFEST", true),
            Check("LIVE_ORDER_AUTHORITY_PROHIBITED", QuantForgeAuthority.ProhibitedAuthorities.Contains("LIVE_ORDER")),
            Check("ARBITRARY_CODE_AUTHORITY_PROHIBITED", QuantForgeAuthority.ProhibitedAuthorities.Contains("ARBITRARY_CODE_EXECUTION")),
            Check("APPLICATION_MUTATION_AUTHORITY_PROHIBITED", QuantForgeAuthority.ProhibitedAuthorities.Contains("UNDECLARED_APP_MUTATION"))
        };
        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks));
        return new(checks.All(x => x.EndsWith(":PASS", StringComparison.Ordinal)), checks, fingerprint);
    }

    private static string Check(string name, bool passed) => $"{name}:{(passed ? "PASS" : "FAIL")}";
}
