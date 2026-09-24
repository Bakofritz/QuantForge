using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record MultiCaseExecutionRequest(
    string BatchId,
    string DeviceId,
    TimeSpan LeaseDuration,
    int MaxCases,
    IReadOnlyList<ResearchContextDescriptor> Cases);

public enum MultiCaseItemState { Pending, Running, Completed, Failed, Canceled }

public sealed record MultiCaseItemResult(
    string ContextId,
    string JobId,
    MultiCaseItemState State,
    string? ResultFingerprint,
    string? ErrorCode);

public sealed record MultiCaseExecutionResult(
    string BatchId,
    IReadOnlyList<MultiCaseItemResult> Items,
    string AggregateFingerprint,
    bool Canceled);

/// <summary>
/// Deterministic coordinator for independent research cases. Execution is intentionally
/// sequential in v20.27; each case gets its own lease and mutable context. Parallel scheduling
/// is deferred until resource accounting and native runtime validation are available.
/// </summary>
public sealed class MultiCaseExecutionCoordinator
{
    private const string ProgressOperation = "MULTI_CASE_COORDINATOR_V20_30";
    private readonly IJobLeaseStore _leases;
    private readonly IResearchJobStore? _jobs;
    private readonly ILocalEvidenceStore? _evidence;
    private readonly ResourceGovernedCaseRunner _resourceRunner;
    private readonly IResourceReceiptStore? _resourceStore;

    private sealed record PersistedCase(
        string ContextId,
        string JobId,
        MultiCaseItemState State,
        string? ResultFingerprint,
        string? ErrorCode);

    public MultiCaseExecutionCoordinator(IJobLeaseStore leases, IResearchJobStore? jobs = null, ILocalEvidenceStore? evidence = null, IResourceReceiptStore? resourceStore = null)
    {
        _leases = leases ?? throw new ArgumentNullException(nameof(leases));
        _jobs = jobs;
        _evidence = evidence;
        _resourceStore = resourceStore;
        _resourceRunner = new ResourceGovernedCaseRunner(_leases);
    }

    public async Task<MultiCaseExecutionResult> ExecuteAsync(
        MultiCaseExecutionRequest request,
        Func<ResearchContextState, CancellationToken, Task<string>> executeCaseAsync,
        MultiCaseResourcePolicy? resourcePolicy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executeCaseAsync);
        ValidateRequest(request);
        var policy = resourcePolicy ?? MultiCaseResourcePolicy.Conservative(request.LeaseDuration);
        var worker = new ResearchWorkerIdentity(request.DeviceId, request.DeviceId, "qf-native-v20.30");
        policy.Validate();
        if (policy.MaxConcurrentCases != 1) throw new InvalidOperationException("MULTI_CASE_PARALLEL_EXECUTION_NOT_YET_ENABLED");

        var isolation = new IsolatedResearchCoordinator();
        var contexts = isolation.CreateBatch(request.Cases);
        var batchFingerprint = CreateBatchFingerprint(request);
        using var batchTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        batchTimeout.CancelAfter(policy.MaxBatchDuration);
        var batchToken = batchTimeout.Token;
        var persisted = await LoadStateAsync(request.BatchId, batchFingerprint, contexts.Count, batchToken);
        var results = persisted.ToDictionary(
            x => x.ContextId,
            x => new MultiCaseItemResult(x.ContextId, x.JobId, x.State, x.ResultFingerprint, x.ErrorCode),
            StringComparer.Ordinal);
        var canceled = false;
        long totalElapsedMilliseconds = 0;
        var totalHeartbeatRenewals = 0;
        for (var index = 0; index < contexts.Count; index++)
        {
            var context = contexts[index];
            var id = context.Descriptor.Identity;

            if (results.TryGetValue(id.ContextId, out var prior) &&
                prior.State is MultiCaseItemState.Completed or MultiCaseItemState.Failed or MultiCaseItemState.Canceled)
                continue;

            if (batchToken.IsCancellationRequested)
            {
                canceled = true;
                break;
            }

            var now = DateTimeOffset.UtcNow;
            var job = ResearchJobLifecycle.Create(id.JobId, id.DatasetFingerprint, id.ConfigurationFingerprint, context.Descriptor.EngineFingerprint, now);
            if (_jobs is not null)
            {
                var existing = await _jobs.LoadJobAsync(id.JobId, batchToken);
                if (existing is not null) job = existing;
                else await _jobs.SaveJobAsync(job, batchToken);
            }

            if (!await _leases.TryAcquireAsync(id.JobId, request.DeviceId, request.LeaseDuration, batchToken))
                throw new InvalidOperationException($"Research case '{id.ContextId}' could not acquire its execution lease.");

            try
            {
                job = ResearchJobLifecycle.Start(job, DateTimeOffset.UtcNow);
                if (_jobs is not null) await _jobs.SaveJobAsync(job, batchToken);

                var startedAt = DateTimeOffset.UtcNow;
                var run = await _resourceRunner.RunAsync(
                    context, request.DeviceId, request.LeaseDuration, policy,
                    token => executeCaseAsync(context, token), batchToken);
                var completedAt = DateTimeOffset.UtcNow;
                totalElapsedMilliseconds += run.ElapsedMilliseconds;
                totalHeartbeatRenewals += run.HeartbeatRenewals;
                var receiptFingerprint = ResearchFingerprint.Sha256(
                    $"{request.BatchId}|{id.ContextId}|{id.JobId}|{worker.IdentityFingerprint}|{MultiCaseItemState.Completed}|{run.ResultFingerprint}|{policy.MaxCaseDuration}|{policy.HeartbeatInterval}");
                var receipt = new MultiCaseExecutionReceipt(
                    request.BatchId, id.ContextId, id.JobId, worker.WorkerId, worker.IdentityFingerprint, MultiCaseItemState.Completed, run.ResultFingerprint,
                    receiptFingerprint, startedAt, completedAt, run.ElapsedMilliseconds, run.HeartbeatRenewals, "WITHIN_POLICY", null);
                // Terminal job state must be durable before a terminal receipt can be committed.
                // This prevents a receipt from claiming completion when the job is still Running.
                await EnsureLeaseOwnershipAsync(id.JobId, request.DeviceId, CancellationToken.None);
                job = ResearchJobLifecycle.Complete(job, DateTimeOffset.UtcNow);
                if (_resourceStore is ITerminalCommitStore)
                {
                    context.AddEvidenceReference($"receipt:{receipt.ReceiptFingerprint}");
                    await CommitTerminalOutcomeAsync(job, receipt, "MULTI_CASE_EXECUTION_RECEIPT", CancellationToken.None);
                }
                else
                {
                    if (_jobs is not null) await _jobs.SaveJobAsync(job, CancellationToken.None);
                    context.AddEvidenceReference($"receipt:{receipt.ReceiptFingerprint}");
                    await CommitTerminalOutcomeAsync(job, receipt, "MULTI_CASE_EXECUTION_RECEIPT", CancellationToken.None);
                }
                var item = new MultiCaseItemResult(id.ContextId, id.JobId, MultiCaseItemState.Completed, run.ResultFingerprint, null);
                results[id.ContextId] = item;
            }
            catch (OperationCanceledException)
            {
                canceled = cancellationToken.IsCancellationRequested || batchToken.IsCancellationRequested;
                var code = cancellationToken.IsCancellationRequested ? "CANCELED" : (batchToken.IsCancellationRequested ? "BATCH_RESOURCE_TIMEOUT" : "CASE_RESOURCE_TIMEOUT");
                var state = canceled ? MultiCaseItemState.Canceled : MultiCaseItemState.Failed;
                var receipt = CreateFailureReceipt(request.BatchId, id.ContextId, id.JobId, worker, state, code);
                await EnsureLeaseOwnershipAsync(id.JobId, request.DeviceId, CancellationToken.None);
                var terminalJob = state == MultiCaseItemState.Canceled
                    ? ResearchJobLifecycle.Cancel(job, DateTimeOffset.UtcNow)
                    : ResearchJobLifecycle.Fail(job, code, DateTimeOffset.UtcNow);
                if (_resourceStore is ITerminalCommitStore)
                    await CommitTerminalOutcomeAsync(terminalJob, receipt, "MULTI_CASE_EXECUTION_RECEIPT", CancellationToken.None);
                else
                {
                    if (_jobs is not null) await _jobs.SaveJobAsync(terminalJob, CancellationToken.None);
                    await CommitTerminalOutcomeAsync(terminalJob, receipt, "MULTI_CASE_EXECUTION_RECEIPT", CancellationToken.None);
                }
                results[id.ContextId] = new MultiCaseItemResult(id.ContextId, id.JobId, state, null, code);
            }
            catch (Exception ex)
            {
                var code = ex.GetType().Name.ToUpperInvariant();
                var receipt = CreateFailureReceipt(request.BatchId, id.ContextId, id.JobId, worker, MultiCaseItemState.Failed, code);
                await EnsureLeaseOwnershipAsync(id.JobId, request.DeviceId, CancellationToken.None);
                var terminalJob = ResearchJobLifecycle.Fail(job, code, DateTimeOffset.UtcNow);
                if (_resourceStore is ITerminalCommitStore)
                    await CommitTerminalOutcomeAsync(terminalJob, receipt, "MULTI_CASE_EXECUTION_RECEIPT", CancellationToken.None);
                else
                {
                    if (_jobs is not null) await _jobs.SaveJobAsync(terminalJob, CancellationToken.None);
                    await CommitTerminalOutcomeAsync(terminalJob, receipt, "MULTI_CASE_EXECUTION_RECEIPT", CancellationToken.None);
                }
                results[id.ContextId] = new MultiCaseItemResult(id.ContextId, id.JobId, MultiCaseItemState.Failed, null, code);
            }
            finally
            {
                await _leases.ReleaseAsync(id.JobId, request.DeviceId, CancellationToken.None);
            }

            await SaveStateAsync(request.BatchId, batchFingerprint, contexts.Count, index + 1, results.Values, CancellationToken.None);

            if (canceled) break;
        }

        var finalItems = contexts
            .Select(c => results.TryGetValue(c.Descriptor.Identity.ContextId, out var item)
                ? item
                : new MultiCaseItemResult(c.Descriptor.Identity.ContextId, c.Descriptor.Identity.JobId, MultiCaseItemState.Pending, null, null))
            .ToList();
        var complete = finalItems.All(x => x.State is MultiCaseItemState.Completed or MultiCaseItemState.Failed or MultiCaseItemState.Canceled);
        var aggregate = CreateAggregateFingerprint(batchFingerprint, finalItems);

        if (canceled || !complete)
            await SaveStateAsync(request.BatchId, batchFingerprint, contexts.Count, finalItems.Count(x => x.State != MultiCaseItemState.Pending), finalItems, CancellationToken.None);

        await SaveBatchResourceRecordAsync(request.BatchId, batchFingerprint, worker, finalItems, totalElapsedMilliseconds, totalHeartbeatRenewals, CancellationToken.None);
        if (_resourceStore is not null)
            await new BatchAccountingReconciler(_resourceStore, _evidence).ReconcileAsync(request.BatchId, batchFingerprint, contexts.Count, CancellationToken.None);
        return new MultiCaseExecutionResult(request.BatchId, finalItems.AsReadOnly(), aggregate, canceled);
    }

    private async Task EnsureLeaseOwnershipAsync(string jobId, string deviceId, CancellationToken cancellationToken)
    {
        if (_leases is not ILeaseInspectionStore inspection)
            throw new InvalidOperationException("LEASE_INTEGRITY_VERIFICATION_UNAVAILABLE");
        var lease = await inspection.LoadLeaseAsync(jobId, cancellationToken);
        if (lease is null || !string.Equals(lease.DeviceId, deviceId, StringComparison.Ordinal) || lease.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("LEASE_INTEGRITY_VIOLATION");
    }

    private async Task CommitTerminalOutcomeAsync(ResearchJob job, MultiCaseExecutionReceipt receipt, string eventType, CancellationToken cancellationToken)
    {
        if (_resourceStore is ITerminalCommitStore terminalStore)
        {
            var evidenceHash = ResearchFingerprint.Sha256($"{receipt.BatchId}|{receipt.ContextId}|{receipt.JobId}|{receipt.WorkerId}|{receipt.WorkerFingerprint}|{receipt.State}|{receipt.ResultFingerprint}|{receipt.ReceiptFingerprint}|{receipt.ResourceStatus}|{receipt.ErrorCode}");
            var request = new TerminalCommitRequest(
                job,
                new TerminalCommitReceipt(receipt.ReceiptFingerprint, receipt.BatchId, receipt.ContextId, receipt.JobId, receipt.WorkerId, receipt.WorkerFingerprint, receipt.State.ToString(), receipt.ResultFingerprint, receipt.StartedAt, receipt.CompletedAt, receipt.ElapsedMilliseconds, receipt.HeartbeatRenewals, receipt.ResourceStatus, receipt.ErrorCode),
                $"execution-receipt:{receipt.ReceiptFingerprint}", evidenceHash,
                $"event:execution-receipt:{receipt.ReceiptFingerprint}", eventType, evidenceHash);
            await terminalStore.CommitTerminalOutcomeAsync(request, cancellationToken);
            return;
        }
        await RecordReceiptAsync(receipt, cancellationToken);
    }

    private async Task RecordReceiptAsync(MultiCaseExecutionReceipt receipt, CancellationToken cancellationToken)
    {
        if (_resourceStore is not null)
        {
            await _resourceStore.AppendExecutionReceiptAsync(new ResourceReceiptRecord(
                receipt.ReceiptFingerprint, receipt.BatchId, receipt.ContextId, receipt.JobId, receipt.WorkerId,
                receipt.WorkerFingerprint, receipt.State.ToString(), receipt.ResultFingerprint, receipt.StartedAt,
                receipt.CompletedAt, receipt.ElapsedMilliseconds, receipt.HeartbeatRenewals, receipt.ResourceStatus, receipt.ErrorCode), cancellationToken);
        }
        if (_evidence is null) return;
        var evidenceHash = ResearchFingerprint.Sha256($"{receipt.BatchId}|{receipt.ContextId}|{receipt.JobId}|{receipt.WorkerId}|{receipt.WorkerFingerprint}|{receipt.State}|{receipt.ResultFingerprint}|{receipt.ReceiptFingerprint}|{receipt.ResourceStatus}|{receipt.ErrorCode}");
        if (!await _evidence.ContainsEvidenceAsync($"execution-receipt:{receipt.ReceiptFingerprint}", cancellationToken))
            await _evidence.AppendEvidenceAsync($"execution-receipt:{receipt.ReceiptFingerprint}", evidenceHash, cancellationToken);
        await _evidence.AppendEventAsync($"event:execution-receipt:{receipt.ReceiptFingerprint}", receipt.JobId, "MULTI_CASE_EXECUTION_RECEIPT", evidenceHash, cancellationToken);
    }

    private static MultiCaseExecutionReceipt CreateFailureReceipt(string batchId, string contextId, string jobId, ResearchWorkerIdentity worker, MultiCaseItemState state, string errorCode)
    {
        var now = DateTimeOffset.UtcNow;
        var resultFingerprint = ResearchFingerprint.Sha256($"{batchId}|{contextId}|{jobId}|{worker.IdentityFingerprint}|{state}|{errorCode}");
        var receiptFingerprint = ResearchFingerprint.Sha256($"{batchId}|{contextId}|{jobId}|{worker.IdentityFingerprint}|{state}|{resultFingerprint}|{errorCode}");
        return new MultiCaseExecutionReceipt(batchId, contextId, jobId, worker.WorkerId, worker.IdentityFingerprint, state, resultFingerprint, receiptFingerprint, now, now, 0, 0, "POLICY_STOP", errorCode);
    }

    private async Task SaveBatchResourceRecordAsync(string batchId, string batchFingerprint, ResearchWorkerIdentity worker, IReadOnlyList<MultiCaseItemResult> items, long totalElapsedMilliseconds, int totalHeartbeatRenewals, CancellationToken cancellationToken)
    {
        if (_resourceStore is null) return;
        var completed = items.Count(x => x.State == MultiCaseItemState.Completed);
        var failed = items.Count(x => x.State == MultiCaseItemState.Failed);
        var canceled = items.Count(x => x.State == MultiCaseItemState.Canceled);
        var fingerprint = ResearchFingerprint.Sha256($"{batchId}|{worker.IdentityFingerprint}|{batchFingerprint}|{items.Count}|{completed}|{failed}|{canceled}|{totalElapsedMilliseconds}|{totalHeartbeatRenewals}");
        var hasCanceled = canceled > 0;
        await _resourceStore.AppendBatchResourceRecordAsync(new BatchResourceRecord(batchId, worker.WorkerId, worker.IdentityFingerprint, batchFingerprint, items.Count, completed, failed, canceled, totalElapsedMilliseconds, totalHeartbeatRenewals, hasCanceled ? "CANCELED" : (failed > 0 ? "COMPLETED_WITH_FAILURES" : (completed == items.Count ? "COMPLETED" : "ACTIVE")), fingerprint, DateTimeOffset.UtcNow), cancellationToken);
    }

    private static void ValidateRequest(MultiCaseExecutionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BatchId)) throw new ArgumentException("BatchId is required.", nameof(request.BatchId));
        if (string.IsNullOrWhiteSpace(request.DeviceId)) throw new ArgumentException("DeviceId is required.", nameof(request.DeviceId));
        if (request.MaxCases < 1) throw new ArgumentOutOfRangeException(nameof(request.MaxCases));
        if (request.Cases.Count > request.MaxCases) throw new InvalidOperationException("Batch exceeds the configured case limit.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in request.Cases)
        {
            if (!ids.Add(c.Identity.ContextId)) throw new InvalidOperationException($"Duplicate context id '{c.Identity.ContextId}'.");
            if (string.IsNullOrWhiteSpace(c.Identity.JobId)) throw new ArgumentException("Every case requires a JobId.", nameof(request));
        }
    }

    private static string CreateBatchFingerprint(MultiCaseExecutionRequest request)
    {
        var cases = request.Cases
            .OrderBy(x => x.Identity.ContextId, StringComparer.Ordinal)
            .Select(x => string.Join("|", new[]
            {
                x.Identity.ContextId, x.Identity.JobId, x.Identity.DatasetId, x.Identity.DatasetFingerprint,
                x.Identity.StrategyId, x.Identity.StrategyFingerprint, x.Identity.Instrument,
                x.Identity.Timeframe, x.Identity.ConfigurationFingerprint, x.EngineFingerprint
            }));
        return ResearchFingerprint.Sha256(string.Join("||", new[] { request.BatchId, request.MaxCases.ToString(System.Globalization.CultureInfo.InvariantCulture), string.Join("@@", cases) }));
    }

    private static string CreateAggregateFingerprint(string batchFingerprint, IEnumerable<MultiCaseItemResult> items)
        => ResearchFingerprint.Sha256(batchFingerprint + "||" + string.Join("@@", items.OrderBy(x => x.ContextId, StringComparer.Ordinal).Select(x => $"{x.ContextId}|{x.JobId}|{x.State}|{x.ResultFingerprint}|{x.ErrorCode}")));

    private async Task<IReadOnlyList<PersistedCase>> LoadStateAsync(string batchId, string batchFingerprint, int total, CancellationToken cancellationToken)
    {
        if (_jobs is null) return Array.Empty<PersistedCase>();
        var progress = await _jobs.LoadProgressAsync(batchId, ProgressOperation, cancellationToken);
        if (progress is null) return Array.Empty<PersistedCase>();
        if (!string.Equals(progress.ConfigurationFingerprint, batchFingerprint, StringComparison.Ordinal) || progress.Total != total)
            throw new InvalidOperationException("MULTI_CASE_CHECKPOINT_FINGERPRINT_MISMATCH");
        var expectedHash = ResearchFingerprint.Sha256($"{progress.JobId}|{progress.Operation}|{progress.Cursor}|{progress.Total}|{progress.DatasetFingerprint}|{progress.ConfigurationFingerprint}|{progress.EngineFingerprint}|{progress.AggregationState}");
        if (!string.Equals(expectedHash, progress.ProgressHash, StringComparison.Ordinal))
            throw new InvalidOperationException("MULTI_CASE_CHECKPOINT_INTEGRITY_FAILURE");
        return System.Text.Json.JsonSerializer.Deserialize<List<PersistedCase>>(progress.AggregationState) ?? new List<PersistedCase>();
    }

    private async Task SaveStateAsync(string batchId, string batchFingerprint, int total, int cursor, IEnumerable<MultiCaseItemResult> items, CancellationToken cancellationToken)
    {
        if (_jobs is null) return;
        var persisted = items.OrderBy(x => x.ContextId, StringComparer.Ordinal)
            .Select(x => new PersistedCase(x.ContextId, x.JobId, x.State, x.ResultFingerprint, x.ErrorCode)).ToList();
        var state = System.Text.Json.JsonSerializer.Serialize(persisted);
        var hash = ResearchFingerprint.Sha256($"{batchId}|{batchFingerprint}|{cursor}|{total}|{state}");
        await _jobs.SaveProgressAsync(new ResearchProgress(batchId, ProgressOperation, cursor, total, "MULTI_CASE", batchFingerprint, "qf-native-v20.28", state, hash, DateTimeOffset.UtcNow), cancellationToken);
        await _jobs.SaveCheckpointAsync(new ResearchCheckpoint(batchId, cursor, cursor, "MULTI_CASE", batchFingerprint, "qf-native-v20.28", hash, DateTimeOffset.UtcNow), cancellationToken);
    }
}
