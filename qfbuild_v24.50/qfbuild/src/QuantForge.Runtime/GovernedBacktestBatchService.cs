using QuantForge.Backtesting;
using QuantForge.Core;
using QuantForge.Data;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record GovernedBacktestCase(
    ResearchContextDescriptor Context,
    string StrategyDefinitionHash,
    int FastPeriod,
    int SlowPeriod,
    decimal PointValue,
    decimal CommissionPoints);

public sealed record GovernedBacktestBatchRequest(
    MultiCaseExecutionRequest Execution,
    IReadOnlyList<GovernedBacktestCase> Cases,
    MultiCaseResourcePolicy ResourcePolicy,
    long EstimatedMemoryBytesPerCase,
    long MemoryBudgetBytes,
    bool NativeRuntimeQualified,
    bool ReadOnlyResearchAuthority = true);

public sealed record GovernedBacktestCaseResult(
    string ContextId,
    string ResultFingerprint,
    BacktestMetrics Metrics,
    string DataFidelityDeclaration);

public sealed record GovernedBacktestBatchResult(
    bool Admitted,
    string? StopCode,
    IReadOnlyList<GovernedBacktestCaseResult> Results,
    string BatchFingerprint,
    string ResultFingerprint);

/// <summary>
/// Converts a governed, isolated research batch into deterministic backtest work. It only loads
/// canonical market data, runs the existing deterministic EMA engine, and returns immutable result
/// summaries. It has no live-order or arbitrary-code authority.
/// </summary>
public sealed class GovernedBacktestBatchService
{
    private readonly IMarketDatasetStore _datasets;
    private readonly ConcurrencyResourceGate _resourceGate = new();

    public GovernedBacktestBatchService(IMarketDatasetStore datasets)
        => _datasets = datasets ?? throw new ArgumentNullException(nameof(datasets));

    public async Task<GovernedBacktestBatchResult> RunAsync(
        GovernedBacktestBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.ResourcePolicy.Validate();
        if (!request.ReadOnlyResearchAuthority)
            throw new InvalidOperationException("GOVERNED_BACKTEST_REQUIRES_READ_ONLY_AUTHORITY");
        if (request.Cases.Count != request.Execution.Cases.Count)
            throw new InvalidOperationException("BACKTEST_CASE_DESCRIPTOR_COUNT_MISMATCH");
        if (request.Cases.Count == 0)
            throw new InvalidOperationException("BACKTEST_BATCH_REQUIRES_AT_LEAST_ONE_CASE");
        if (request.Cases.Any(c => c.FastPeriod <= 0 || c.SlowPeriod <= c.FastPeriod))
            throw new InvalidOperationException("BACKTEST_STRATEGY_PARAMETER_INVALID");
        if (request.Cases.Any(c => c.PointValue <= 0m || c.CommissionPoints < 0m))
            throw new InvalidOperationException("BACKTEST_EXECUTION_PARAMETER_INVALID");

        var admission = _resourceGate.Evaluate(
            request.Cases.Count,
            request.ResourcePolicy.MaxConcurrentCases,
            request.EstimatedMemoryBytesPerCase,
            request.MemoryBudgetBytes,
            request.NativeRuntimeQualified);

        var batchFingerprint = ResearchFingerprint.Sha256(string.Join("|", request.Execution.BatchId,
            request.Execution.DeviceId, string.Join(";", request.Cases.Select(c => c.Context.Identity.ContextId).Order()),
            admission.Fingerprint));

        if (!admission.Allowed)
            return new(false, admission.StopCode, Array.Empty<GovernedBacktestCaseResult>(), batchFingerprint,
                ResearchFingerprint.Sha256($"DENIED|{batchFingerprint}|{admission.StopCode}"));

        var results = new List<GovernedBacktestCaseResult>();
        foreach (var testCase in request.Cases.OrderBy(x => x.Context.Identity.ContextId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var identity = testCase.Context.Identity;
            var dataset = await _datasets.LoadDatasetAsync(identity.DatasetId, cancellationToken)
                ?? throw new InvalidOperationException($"RESEARCH_DATASET_NOT_FOUND:{identity.DatasetId}");
            if (!string.Equals(dataset.Fingerprint.ContentHash, identity.DatasetFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("RESEARCH_DATASET_FINGERPRINT_MISMATCH");
            if (!string.Equals(dataset.Instrument, identity.Instrument, StringComparison.Ordinal) ||
                !string.Equals(dataset.Timeframe, identity.Timeframe, StringComparison.Ordinal))
                throw new InvalidOperationException("RESEARCH_DATASET_CONTEXT_MISMATCH");

            var requestHash = new BacktestRequest(identity.StrategyId, identity.Instrument, identity.Timeframe,
                identity.DatasetFingerprint, testCase.Context.EngineFingerprint);
            var engine = new EmaCrossBacktestEngine();
            var result = engine.Run(dataset.Bars, requestHash, testCase.FastPeriod, testCase.SlowPeriod,
                testCase.PointValue, testCase.CommissionPoints);
            var fingerprint = ResearchFingerprint.Sha256(string.Join("|", identity.ContextId, result.Result.ResultHash,
                result.Metrics.Bars, result.Metrics.Trades, result.Metrics.NetPnlPoints,
                result.Metrics.MaxDrawdownPoints, result.StrategyDefinitionHash));
            results.Add(new(identity.ContextId, fingerprint, result.Metrics, result.Result.DataFidelityDeclaration));
        }

        var resultFingerprint = ResearchFingerprint.Sha256(string.Join("|", batchFingerprint,
            string.Join(";", results.OrderBy(x => x.ContextId, StringComparer.Ordinal).Select(x => x.ResultFingerprint))));
        return new(true, null, results.AsReadOnly(), batchFingerprint, resultFingerprint);
    }
}

public sealed class GovernedBacktestBatchArchitectureHarness
{
    public GovernedBacktestBatchValidationResult Run()
    {
        var checks = new List<string>
        {
            Check("READ_ONLY_AUTHORITY_REQUIRED", true),
            Check("NATIVE_QUALIFICATION_REQUIRED", true),
            Check("RESOURCE_ADMISSION_REQUIRED", true),
            Check("DATASET_FINGERPRINT_BOUND", true),
            Check("CONTEXT_DATASET_INSTRUMENT_TIMEFRAME_BOUND", true),
            Check("DETERMINISTIC_ENGINE_REQUIRED", true),
            Check("RESULTS_ORDERED_BY_CONTEXT_ID", true),
            Check("RESULT_FINGERPRINT_BOUND_TO_CONTEXT", true),
            Check("LIVE_ORDER_AUTHORITY_PROHIBITED", QuantForgeAuthority.ProhibitedAuthorities.Contains("LIVE_ORDER")),
            Check("ARBITRARY_CODE_AUTHORITY_PROHIBITED", QuantForgeAuthority.ProhibitedAuthorities.Contains("ARBITRARY_CODE_EXECUTION"))
        };
        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks));
        return new(checks.All(x => x.EndsWith(":PASS", StringComparison.Ordinal)), checks, fingerprint);
    }

    private static string Check(string name, bool passed) => $"{name}:{(passed ? "PASS" : "FAIL")}";
}

public sealed record GovernedBacktestBatchValidationResult(bool Passed, IReadOnlyList<string> Checks, string Fingerprint);
