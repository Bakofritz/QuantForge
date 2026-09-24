using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuantForge.Backtesting;
using QuantForge.Data;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record ResearchRunRequest(
    string JobId,
    string DeviceId,
    string DatasetId,
    string StrategyId,
    string Instrument,
    string Timeframe,
    TimeSpan LeaseDuration,
    int FastPeriod = 9,
    int SlowPeriod = 21,
    decimal PointValue = 1m,
    decimal CommissionPoints = 0m);

public sealed record ResearchRunOutcome(
    string JobId,
    string DatasetFingerprint,
    string RunId,
    BacktestMetrics Metrics,
    bool EvidenceRecorded);

/// <summary>
/// First native end-to-end governed research path. It deliberately has no trading authority.
/// </summary>
public sealed class ResearchRuntime
{
    private readonly IMarketDatasetStore _datasets;
    private readonly ILocalEvidenceStore _evidence;
    private readonly IJobLeaseStore _leases;
    private readonly EmaCrossBacktestEngine _engine;
    private readonly CanonicalDataGate _dataGate;
    private readonly IsolatedResearchCoordinator _isolation = new();
    private readonly MultiCaseExecutionCoordinator _multiCase;
    private readonly BatchAccountingReconciler? _batchAccounting;
    private readonly WorkerReconciliationService? _reconciliation;
    private readonly RecoveryContinuationService? _recoveryContinuation;
    private readonly CheckpointBoundContinuationService? _checkpointBoundContinuation;
    private readonly BatchManifestIntegrityService? _batchManifestIntegrity;
    private readonly TerminalStateAgreementService? _terminalStateAgreement;
    private readonly ControlledConcurrencyReadinessService? _controlledConcurrencyReadiness;
    private readonly ConcurrencySafetyGateService? _concurrencySafetyGate;
    private readonly ConcurrencyValidationHarness _concurrencyValidationHarness = new();
    private readonly StorageFaultValidationService _storageFaultValidation = new();
    private readonly SqliteStorageFaultExecutionAdapter _sqliteStorageFaultExecution = new();
    private readonly StorageFaultRecoveryValidationService _storageFaultRecoveryValidation;
    private readonly TransactionalTerminalFaultValidationService _transactionalTerminalFaultValidation = new();
    private readonly RecoveryStateIntegrityService? _recoveryStateIntegrity;
    private readonly WorkerRecoveryIntegrityService? _workerRecoveryIntegrity;
    private readonly TerminalCommitIdempotencyValidationService _terminalCommitIdempotencyValidation = new();
    private readonly IRecoveryPreflightStore? _recoveryPreflightStore;
    private readonly RecoveryPreflightBindingService? _recoveryPreflightBinding;
    private readonly RecoveryPreflightBindingFaultValidationService? _recoveryPreflightBindingFaultValidation;
    private readonly RecoveryBindingReconciliationService? _recoveryBindingReconciliation;
    private readonly RecoveryBindingReconciliationFaultValidationService _recoveryBindingReconciliationFaultValidation;
    private readonly RecoveryContinuationGateService? _recoveryContinuationGate;

    public ResearchRuntime(
        IMarketDatasetStore datasets,
        ILocalEvidenceStore evidence,
        IJobLeaseStore leases,
        EmaCrossBacktestEngine engine,
        CanonicalDataGate? dataGate = null,
        IResearchJobStore? jobs = null,
        IResourceReceiptStore? resourceStore = null)
    {
        _datasets = datasets;
        _evidence = evidence;
        _leases = leases;
        _engine = engine;
        _dataGate = dataGate ?? new CanonicalDataGate();
        _multiCase = new MultiCaseExecutionCoordinator(_leases, jobs, _evidence, resourceStore);
        _recoveryPreflightStore = resourceStore as IRecoveryPreflightStore;
        _storageFaultRecoveryValidation = new StorageFaultRecoveryValidationService(_sqliteStorageFaultExecution);
        _recoveryBindingReconciliationFaultValidation = new RecoveryBindingReconciliationFaultValidationService(_evidence);
        if (resourceStore is not null) _batchAccounting = new BatchAccountingReconciler(resourceStore, _evidence);
        if (jobs is not null && _leases is ILeaseInspectionStore leaseInspection && resourceStore is not null && resourceStore is IRecoveryAuditStore audits)
        {
            _reconciliation = new WorkerReconciliationService(jobs, leaseInspection, resourceStore, audits, _evidence);
            _recoveryContinuation = new RecoveryContinuationService(jobs, leaseInspection, _leases, resourceStore, audits, _evidence, _recoveryPreflightStore);
            _checkpointBoundContinuation = new CheckpointBoundContinuationService(jobs, leaseInspection, _leases, resourceStore, audits, _evidence, _recoveryPreflightStore, resourceStore as IRecoveryContinuationReplayStore);
            _batchManifestIntegrity = new BatchManifestIntegrityService(resourceStore, jobs, _evidence);
            _terminalStateAgreement = new TerminalStateAgreementService(jobs, resourceStore, _evidence);
            _recoveryStateIntegrity = new RecoveryStateIntegrityService(jobs, leaseInspection, resourceStore);
            _workerRecoveryIntegrity = new WorkerRecoveryIntegrityService(jobs, leaseInspection, resourceStore);
            if (_recoveryPreflightStore is not null)
            {
                _recoveryPreflightBinding = new RecoveryPreflightBindingService(_recoveryPreflightStore, jobs, leaseInspection);
                _recoveryBindingReconciliation = new RecoveryBindingReconciliationService(_recoveryPreflightBinding, audits, _evidence);
                if (audits is IRecoveryBindingReconciliationVerificationStore verification)
                    _recoveryContinuationGate = new RecoveryContinuationGateService(_recoveryContinuation!, _recoveryBindingReconciliation, verification, jobs, _leases);
            }
            _recoveryPreflightBindingFaultValidation = new RecoveryPreflightBindingFaultValidationService(_evidence);
            _controlledConcurrencyReadiness = new ControlledConcurrencyReadinessService(jobs, resourceStore, _evidence);
            _concurrencySafetyGate = new ConcurrencySafetyGateService(jobs, resourceStore, _evidence);
        }
    }


    public Task<TerminalCommitIdempotencyValidationResult> ValidateTerminalCommitIdempotencyAsync(string databasePath, CancellationToken cancellationToken = default)
        => _terminalCommitIdempotencyValidation.ValidateAsync(databasePath, _evidence, cancellationToken);


    public IReadOnlyList<ResearchContextState> CreateIsolatedResearchCases(
        IEnumerable<IsolatedResearchCaseRequest> cases, string engineFingerprint = "qf-native-v20.26")
        => _isolation.CreateBatch(cases.Select(c => _isolation.Create(c, engineFingerprint).Descriptor));

    public Task<WorkerRecoveryIntegrityResult> ValidateWorkerRecoveryIntegrityAsync(
        string jobId, string expectedWorkerDeviceId, CancellationToken cancellationToken = default)
        => _workerRecoveryIntegrity is null
            ? throw new InvalidOperationException("WORKER_RECOVERY_INTEGRITY_REQUIRES_DURABLE_JOB_RESOURCE_AND_LEASE_INSPECTION_STORES")
            : _workerRecoveryIntegrity.ValidateAsync(jobId, expectedWorkerDeviceId, cancellationToken);

    public Task<IReadOnlyList<RecoveryPreflightReceipt>> LoadRecoveryPreflightsAsync(
        string jobId, CancellationToken cancellationToken = default)
        => _recoveryPreflightStore is null
            ? throw new InvalidOperationException("RECOVERY_PREFLIGHT_REQUIRES_A_RECOVERY_PREFLIGHT_STORE")
            : _recoveryPreflightStore.LoadRecoveryPreflightsForJobAsync(jobId, cancellationToken);

    public Task<RecoveryReconciliationResult> ReconcileWorkerAsync(string batchId, string contextId, string jobId, string workerFingerprint, CancellationToken cancellationToken = default)
        => _reconciliation is null
            ? throw new InvalidOperationException("RECOVERY_RECONCILIATION_REQUIRES_DURABLE_JOB_RESOURCE_AND_AUDIT_STORES")
            : _reconciliation.ReconcileAsync(batchId, contextId, jobId, workerFingerprint, cancellationToken);

    public Task<RecoveryBindingFaultValidationResult> ValidateRecoveryPreflightBindingFaultsAsync(CancellationToken cancellationToken = default)
        => _recoveryPreflightBindingFaultValidation is null
            ? throw new InvalidOperationException("RECOVERY_BINDING_FAULT_VALIDATION_REQUIRES_STORAGE")
            : _recoveryPreflightBindingFaultValidation.ValidateAsync(cancellationToken);

    public Task<RecoveryPreflightBindingResult> ValidateRecoveryPreflightBindingAsync(
        RecoveryContinuationRequest request, CancellationToken cancellationToken = default)
        => _recoveryPreflightBinding is null
            ? throw new InvalidOperationException("RECOVERY_PREFLIGHT_BINDING_REQUIRES_DURABLE_JOB_LEASE_AND_PREFLIGHT_STORES")
            : _recoveryPreflightBinding.ValidateAsync(request, cancellationToken);

    public Task<RecoveryBindingReconciliationFaultValidationResult> ValidateRecoveryBindingReconciliationFaultsAsync(CancellationToken cancellationToken = default)
        => _recoveryBindingReconciliationFaultValidation.ValidateAsync(cancellationToken);

    public async Task<RecoveryBindingReconciliationNativeFaultValidationResult> ValidateNativeRecoveryBindingReconciliationFaultsAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var adapter = new QuantForge.Storage.RecoveryBindingReconciliationNativeFaultAdapter();
        var result = await adapter.RunAsync(databasePath, cancellationToken);
        await _evidence.AppendEvidenceAsync($"sqlite-recovery-binding-reconciliation-fault:{result.ResultFingerprint}", result.ResultFingerprint, cancellationToken);
        await _evidence.AppendEventAsync($"event:sqlite-recovery-binding-reconciliation-fault:{result.ResultFingerprint}", "recovery-binding-reconciliation-fault", "SQLITE_RECOVERY_BINDING_RECONCILIATION_FAULT_VALIDATION", result.ResultFingerprint, cancellationToken);
        return result;
    }

    public Task<RecoveryBindingReconciliationResult> ReconcileRecoveryBindingAsync(
        RecoveryContinuationRequest request, CancellationToken cancellationToken = default)
        => _recoveryBindingReconciliation is null
            ? throw new InvalidOperationException("RECOVERY_BINDING_RECONCILIATION_REQUIRES_DURABLE_JOB_LEASE_PREFLIGHT_AND_AUDIT_STORES")
            : _recoveryBindingReconciliation.ReconcileAsync(request, cancellationToken);

    public Task<RecoveryContinuationResult> PrepareRecoveryContinuationAsync(
        RecoveryContinuationRequest request, CancellationToken cancellationToken = default)
        => _recoveryContinuation is null
            ? throw new InvalidOperationException("RECOVERY_CONTINUATION_REQUIRES_DURABLE_JOB_RESOURCE_AND_AUDIT_STORES")
            : _recoveryContinuation.PrepareAsync(request, cancellationToken);

    public Task<RecoveryContinuationResult> PrepareAndAuthorizeRecoveryContinuationAsync(
        RecoveryContinuationRequest request, CancellationToken cancellationToken = default)
        => _recoveryContinuationGate is null
            ? throw new InvalidOperationException("RECOVERY_CONTINUATION_GATE_REQUIRES_ATOMIC_RECONCILIATION_READBACK_STORE")
            : _recoveryContinuationGate.PrepareAndAuthorizeAsync(request, cancellationToken);

    public Task<CheckpointContinuationOutcome> ExecuteRecoveryContinuationAsync(
        RecoveryContinuationRequest request,
        Func<CheckpointContinuationCursor, CancellationToken, Task<CheckpointContinuationStep>> executeStepAsync,
        int maxSteps = 100_000,
        CancellationToken cancellationToken = default)
        => _checkpointBoundContinuation is null
            ? throw new InvalidOperationException("CHECKPOINT_CONTINUATION_REQUIRES_DURABLE_JOB_RESOURCE_AND_AUDIT_STORES")
            : _checkpointBoundContinuation.ExecuteAsync(request, executeStepAsync, maxSteps, cancellationToken);

    public Task<BatchManifestIntegrityResult> ValidateBatchManifestAsync(ResearchBatchManifest manifest, CancellationToken cancellationToken = default)
        => _batchManifestIntegrity is null
            ? throw new InvalidOperationException("BATCH_MANIFEST_INTEGRITY_REQUIRES_DURABLE_JOB_AND_RESOURCE_STORES")
            : _batchManifestIntegrity.ValidateAsync(manifest, cancellationToken);

    public Task<TerminalStateAgreementResult> ValidateTerminalStateAgreementAsync(ResearchBatchManifest manifest, CancellationToken cancellationToken = default)
        => _terminalStateAgreement is null
            ? throw new InvalidOperationException("TERMINAL_STATE_AGREEMENT_REQUIRES_DURABLE_JOB_AND_RESOURCE_STORES")
            : _terminalStateAgreement.ValidateAsync(manifest, cancellationToken);

    public Task<ControlledConcurrencyReadinessResult> ReviewControlledConcurrencyAsync(ResearchBatchManifest manifest, CancellationToken cancellationToken = default)
        => _controlledConcurrencyReadiness is null
            ? throw new InvalidOperationException("CONTROLLED_CONCURRENCY_REVIEW_REQUIRES_DURABLE_JOB_AND_RESOURCE_STORES")
            : _controlledConcurrencyReadiness.ReviewAsync(manifest, cancellationToken);

    public Task<ConcurrencySafetyGateResult> EvaluateConcurrencySafetyAsync(ResearchBatchManifest manifest, ConcurrencyResourceBudget budget, CancellationToken cancellationToken = default)
        => _concurrencySafetyGate is null
            ? throw new InvalidOperationException("CONCURRENCY_SAFETY_GATE_REQUIRES_DURABLE_JOB_AND_RESOURCE_STORES")
            : _concurrencySafetyGate.EvaluateAsync(manifest, budget, cancellationToken);

    public Task<ConcurrencyValidationResult> ValidateConcurrencyHarnessAsync(CancellationToken cancellationToken = default)
        => _concurrencyValidationHarness.RunAsync(cancellationToken);

    public Task<RecoveryStateIntegrityResult> ValidateRecoveryStateIntegrityAsync(
        string jobId, string expectedWorkerDeviceId, CancellationToken cancellationToken = default)
        => _recoveryStateIntegrity is null
            ? throw new InvalidOperationException("RECOVERY_STATE_INTEGRITY_REQUIRES_DURABLE_JOB_RESOURCE_AND_LEASE_INSPECTION_STORES")
            : _recoveryStateIntegrity.ValidateAsync(jobId, expectedWorkerDeviceId, cancellationToken);

    public Task<StorageFaultInjectionResult> ValidateStorageFaultBoundaryAsync(CancellationToken cancellationToken = default)
        => _storageFaultValidation.ValidateAsync(_evidence, cancellationToken);



    public Task<TransactionalTerminalFaultValidationResult> ValidateTransactionalTerminalFaultAsync(
        string databasePath, CancellationToken cancellationToken = default)
        => _transactionalTerminalFaultValidation.ValidateAsync(databasePath, _evidence, cancellationToken);
    public Task<StorageFaultRecoveryValidationResult> ValidateNativeStorageFaultRecoveryAsync(
        string databasePath, CancellationToken cancellationToken = default)
        => _storageFaultRecoveryValidation.ValidateAsync(databasePath, _evidence, cancellationToken);

    public async Task<StorageFaultInjectionResult> ValidateNativeStorageFaultBoundaryAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        var result = await _sqliteStorageFaultExecution.RunAsync(databasePath, cancellationToken);
        await _evidence.AppendEvidenceAsync($"sqlite-storage-fault-validation:{result.ResultFingerprint}", result.ResultFingerprint, cancellationToken);
        await _evidence.AppendEventAsync($"event:sqlite-storage-fault-validation:{result.ResultFingerprint}", "STORAGE_FAULT_VALIDATION", "SQLITE_STORAGE_FAULT_VALIDATION_COMPLETED", result.ResultFingerprint, cancellationToken);
        return result;
    }

    public async Task<StorageConcurrencyValidationResult> ValidateStorageConcurrencyAsync(
        IConcurrencyStorageValidationAdapter adapter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        var result = await adapter.RunAsync(cancellationToken);
        if (_evidence is IConcurrencyValidationResultStore resultStore)
        {
            await resultStore.AppendConcurrencyValidationResultAsync(result, cancellationToken);
            await _evidence.AppendEvidenceAsync($"concurrency-storage-validation:{result.ResultFingerprint}", result.ResultFingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:concurrency-storage-validation:{result.ResultFingerprint}", "CONCURRENCY_VALIDATION", "SQLITE_CONCURRENCY_VALIDATION_COMPLETED", result.ResultFingerprint, cancellationToken);
        }
        return result;
    }

    public Task<BatchAccountingReconciliationResult> ReconcileBatchAccountingAsync(
        string batchId, string batchFingerprint, int expectedCaseCount, CancellationToken cancellationToken = default)
        => _batchAccounting is null
            ? throw new InvalidOperationException("BATCH_ACCOUNTING_REQUIRES_DURABLE_RESOURCE_STORE")
            : _batchAccounting.ReconcileAsync(batchId, batchFingerprint, expectedCaseCount, cancellationToken);

    public Task<MultiCaseExecutionResult> ExecuteMultiCaseAsync(
        MultiCaseExecutionRequest request,
        Func<ResearchContextState, CancellationToken, Task<string>> executeCaseAsync,
        MultiCaseResourcePolicy? resourcePolicy = null,
        CancellationToken cancellationToken = default)
        => _multiCase.ExecuteAsync(request, executeCaseAsync, resourcePolicy, cancellationToken);

    public async Task RecordPositionLifecycleEvidenceAsync(string jobId, PositionLifecycleResult result, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentNullException.ThrowIfNull(result);
        var evidenceHash = ResearchFingerprint.Sha256($"{result.EngineFingerprint}|{result.EvidenceFingerprint}|{result.FinalLedger.Fingerprint}");
        await _evidence.AppendEvidenceAsync($"position-lifecycle:{jobId}", evidenceHash, cancellationToken);
        if (_evidence is ILedgerEvidenceStore ledgerStore)
        {
            foreach (var trade in result.Trades)
                await ledgerStore.AppendTradeEvidenceAsync(jobId, trade.TradeId, trade.EntryTime.ToUniversalTime().ToString("O"), trade.ExitTime.ToUniversalTime().ToString("O"), trade.Quantity, trade.EntryPrice, trade.ExitPrice, trade.GrossPnl, trade.Commission, trade.SlippageCost, trade.NetPnl, trade.ExitReason, trade.Fingerprint, cancellationToken);
            foreach (var evt in result.LedgerEvents)
                await ledgerStore.AppendLedgerAccountingEventAsync(jobId, evt.Sequence, evt.Timestamp.ToUniversalTime().ToString("O"), evt.SessionKey, evt.Type, evt.PositionBefore, evt.PositionAfter, evt.RealizedPnl, evt.DailyRealizedPnl, evt.Equity, evt.DailyLossLocked, evt.ReferenceId, evt.Details, evt.Fingerprint, cancellationToken);
        }
        await _evidence.AppendEventAsync($"event:position-lifecycle:{jobId}", jobId, "POSITION_LIFECYCLE_EVIDENCE_RECORDED", evidenceHash, cancellationToken);
    }

    public async Task<ResearchRunOutcome> RunAsync(ResearchRunRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _leases.TryAcquireAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
            throw new InvalidOperationException("Research job is already leased by another device or worker.");

        try
        {
            var dataset = await _datasets.LoadDatasetAsync(request.DatasetId, cancellationToken)
                ?? throw new InvalidOperationException($"Dataset '{request.DatasetId}' was not found in local storage.");

            var gate = _dataGate.Validate(dataset);
            if (!gate.Allowed) throw new InvalidOperationException("Canonical data gate blocked research execution: " + string.Join(",", gate.Errors));

            if (!string.Equals(dataset.Instrument, request.Instrument, StringComparison.Ordinal) ||
                !string.Equals(dataset.Timeframe, request.Timeframe, StringComparison.Ordinal))
                throw new InvalidOperationException("Research request does not match the stored dataset identity.");

            var backtestRequest = new BacktestRequest(
                request.StrategyId,
                request.Instrument,
                request.Timeframe,
                dataset.Fingerprint.ContentHash,
                "qf-native-v20.5");

            var result = _engine.Run(dataset.Bars, backtestRequest, request.FastPeriod, request.SlowPeriod,
                request.PointValue, request.CommissionPoints);

            var evidenceHash = Hash($"{result.Result.ResultHash}|{dataset.Fingerprint.ContentHash}|{result.StrategyDefinitionHash}");
            var evidenceId = $"backtest:{result.Result.RunId}";
            await _evidence.AppendEvidenceAsync(evidenceId, evidenceHash, cancellationToken);
            await _evidence.AppendEventAsync(
                $"event:{result.Result.RunId}",
                request.JobId,
                "BACKTEST_COMPLETED",
                evidenceHash,
                cancellationToken);

            return new ResearchRunOutcome(
                request.JobId,
                dataset.Fingerprint.ContentHash,
                result.Result.RunId,
                result.Metrics,
                true);
        }
        finally
        {
            await _leases.ReleaseAsync(request.JobId, request.DeviceId, CancellationToken.None);
        }
    }

    public async Task<ExecutionSensitivityReport> RunExecutionSensitivityAsync(
        ResearchRunRequest request, IEnumerable<ExecutionScenario> scenarios, CancellationToken cancellationToken = default)
    {
        if (!await _leases.TryAcquireAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
            throw new InvalidOperationException("Research job is already leased by another device or worker.");
        try
        {
            var dataset = await _datasets.LoadDatasetAsync(request.DatasetId, cancellationToken)
                ?? throw new InvalidOperationException($"Dataset '{request.DatasetId}' was not found in local storage.");
            var gate = _dataGate.Validate(dataset);
            if (!gate.Allowed) throw new InvalidOperationException("Canonical data gate blocked execution sensitivity research: " + string.Join(",", gate.Errors));
            if (!string.Equals(dataset.Instrument, request.Instrument, StringComparison.Ordinal) ||
                !string.Equals(dataset.Timeframe, request.Timeframe, StringComparison.Ordinal))
                throw new InvalidOperationException("Research request does not match the stored dataset identity.");

            var baseline = TradingRiskSettings.DefaultMes();
            if (!string.Equals(baseline.PrimaryInstrument, request.Instrument, StringComparison.Ordinal))
                throw new InvalidOperationException("The governed v20.18 sensitivity default is MES; an explicit instrument profile is required before another instrument is enabled.");

            var scenarioArray = scenarios.ToArray();
            var configHash = ResearchFingerprint.Sha256($"EXECUTION_SENSITIVITY|{request.StrategyId}|{request.Instrument}|{request.Timeframe}|{dataset.Fingerprint.ContentHash}|{string.Join("|", scenarioArray.Select(x => $"{x.ScenarioId}:{x.LatencyMs}:{x.SlippageTicksPerSide}:{x.ConnectionProfile}"))}");
            const string engineVersion = "qf-native-v20.18";
            var report = ExecutionSensitivityAnalyzer.Analyze(baseline, scenarioArray);
            var evidenceHash = ResearchFingerprint.Sha256($"{report.ReportFingerprint}|{dataset.Fingerprint.ContentHash}|{configHash}|{engineVersion}");
            await _evidence.AppendEvidenceAsync($"execution-sensitivity:{request.JobId}", evidenceHash, cancellationToken);
            await _evidence.AppendEventAsync($"event:execution-sensitivity:{request.JobId}", request.JobId, "EXECUTION_SENSITIVITY_COMPLETED", evidenceHash, cancellationToken);
            return report;
        }
        finally
        {
            await _leases.ReleaseAsync(request.JobId, request.DeviceId, CancellationToken.None);
        }
    }

    public async Task<RobustnessReport> RunRobustnessAsync(
        ResearchRunRequest request, int baselineFast, int baselineSlow, int radius, CancellationToken cancellationToken = default)
    {
        if (!await _leases.TryAcquireAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
            throw new InvalidOperationException("Research job is already leased by another device or worker.");
        try
        {
            var dataset = await _datasets.LoadDatasetAsync(request.DatasetId, cancellationToken)
                ?? throw new InvalidOperationException($"Dataset '{request.DatasetId}' was not found in local storage.");
            var gate = _dataGate.Validate(dataset);
            if (!gate.Allowed) throw new InvalidOperationException("Canonical data gate blocked robustness execution: " + string.Join(",", gate.Errors));
            var configHash = Hash($"ROBUSTNESS|{request.StrategyId}|{request.Instrument}|{request.Timeframe}|{baselineFast}|{baselineSlow}|{radius}|{request.PointValue}|{request.CommissionPoints}");
            var job = new ResearchJob(request.JobId, DurableJobState.Running, dataset.Fingerprint.ContentHash, configHash, "qf-native-v20.7", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            if (_evidence is IResearchJobStore jobs) await jobs.SaveJobAsync(job, cancellationToken);
            if (_evidence is IResearchJobStore checkpointStore)
                await checkpointStore.SaveCheckpointAsync(new ResearchCheckpoint(request.JobId, 1, 0, dataset.Fingerprint.ContentHash, configHash, "qf-native-v20.7", configHash, DateTimeOffset.UtcNow), cancellationToken);
            var robustnessAnalyzer = new DeterministicRobustnessAnalyzer();
            var report = robustnessAnalyzer.Run(request.JobId, dataset.Bars, dataset.Fingerprint.ContentHash, request.Instrument, request.Timeframe, "qf-native-v20.7", baselineFast, baselineSlow, radius, request.PointValue, request.CommissionPoints);
            var evidenceHash = Hash($"{report.StabilityFingerprint}|{dataset.Fingerprint.ContentHash}|{configHash}");
            await _evidence.AppendEvidenceAsync($"robustness:{request.JobId}", evidenceHash, cancellationToken);
            await _evidence.AppendEventAsync($"event:robustness:{request.JobId}", request.JobId, "ROBUSTNESS_COMPLETED", evidenceHash, cancellationToken);
            if (_evidence is IResearchJobStore completedStore)
                await completedStore.SaveJobAsync(job with { State = DurableJobState.Completed, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken);
            return report;
        }
        finally { await _leases.ReleaseAsync(request.JobId, request.DeviceId, CancellationToken.None); }
    }

    public async Task<MonteCarloReport> RunMonteCarloAsync(
        ResearchRunRequest request, int iterations = 1000, ulong seed = 1, CancellationToken cancellationToken = default)
    {
        if (!await _leases.TryAcquireAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
            throw new InvalidOperationException("Research job is already leased by another device or worker.");
        try
        {
            var dataset = await _datasets.LoadDatasetAsync(request.DatasetId, cancellationToken)
                ?? throw new InvalidOperationException($"Dataset '{request.DatasetId}' was not found in local storage.");
            var gate = _dataGate.Validate(dataset);
            if (!gate.Allowed) throw new InvalidOperationException("Canonical data gate blocked Monte Carlo execution: " + string.Join(",", gate.Errors));
            if (!string.Equals(dataset.Instrument, request.Instrument, StringComparison.Ordinal) ||
                !string.Equals(dataset.Timeframe, request.Timeframe, StringComparison.Ordinal))
                throw new InvalidOperationException("Research request does not match the stored dataset identity.");

            var configHash = Hash($"MONTE_CARLO_BOOTSTRAP|{request.StrategyId}|{request.Instrument}|{request.Timeframe}|{request.FastPeriod}|{request.SlowPeriod}|{iterations}|{seed}|{request.PointValue}|{request.CommissionPoints}");
            const string engineVersion = "qf-native-v20.9";
            var jobs = _evidence as IResearchJobStore;
            var existing = jobs is null ? null : await jobs.LoadJobAsync(request.JobId, cancellationToken);
            if (existing is not null &&
                (!string.Equals(existing.DatasetFingerprint, dataset.Fingerprint.ContentHash, StringComparison.Ordinal) ||
                 !string.Equals(existing.ConfigurationFingerprint, configHash, StringComparison.Ordinal) ||
                 !string.Equals(existing.EngineFingerprint, engineVersion, StringComparison.Ordinal)))
                throw new InvalidOperationException("Existing research job fingerprint does not match the requested Monte Carlo configuration.");

            var analyzer = new DeterministicMonteCarloAnalyzer();
            var progress = jobs is null ? null : await jobs.LoadProgressAsync(request.JobId, "MONTE_CARLO_BOOTSTRAP", cancellationToken);
            DeterministicMonteCarloAnalyzer.MonteCarloExecutionState state;
            if (progress is not null)
            {
                if (progress.Cursor > progress.Total || progress.Total != iterations ||
                    progress.DatasetFingerprint != dataset.Fingerprint.ContentHash || progress.ConfigurationFingerprint != configHash || progress.EngineFingerprint != engineVersion)
                    throw new InvalidOperationException("Stored Monte Carlo progress cannot be resumed because its fingerprint or total differs.");
                var expectedProgressHash = Hash($"{progress.JobId}|{progress.Operation}|{progress.Cursor}|{progress.Total}|{progress.DatasetFingerprint}|{progress.ConfigurationFingerprint}|{progress.EngineFingerprint}|{progress.AggregationState}");
                if (!string.Equals(expectedProgressHash, progress.ProgressHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Stored Monte Carlo progress failed integrity verification.");
                state = DeterministicMonteCarloAnalyzer.DeserializeState(progress.AggregationState);
            }
            else
            {
                state = analyzer.CreateState(dataset.Bars, dataset.Fingerprint.ContentHash, request.Instrument, request.Timeframe, engineVersion,
                    request.FastPeriod, request.SlowPeriod, iterations, seed, request.PointValue, request.CommissionPoints);
            }

            var now = DateTimeOffset.UtcNow;
            var job = existing ?? ResearchJobLifecycle.Create(request.JobId, dataset.Fingerprint.ContentHash, configHash, engineVersion, now);
            job = job.State == DurableJobState.RecoveryRequired || job.State == DurableJobState.Paused || job.State == DurableJobState.Queued
                ? ResearchJobLifecycle.Start(job, now) : job;
            if (job.State != DurableJobState.Running && job.State != DurableJobState.Completed)
                throw new InvalidOperationException($"Monte Carlo job cannot run from state {job.State}.");
            if (jobs is not null) await jobs.SaveJobAsync(job, cancellationToken);

            const int chunkSize = 250;
            while (!state.Cursor.Equals(state.TotalIterations))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cancellation = jobs is null ? null : await jobs.LoadCancellationAsync(request.JobId, cancellationToken);
                if (cancellation is not null)
                {
                    if (jobs is not null) await jobs.SaveJobAsync(ResearchJobLifecycle.Cancel(job, DateTimeOffset.UtcNow), CancellationToken.None);
                    throw new OperationCanceledException($"Research job canceled: {cancellation.Reason}");
                }
                if (!await _leases.RenewAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
                    throw new InvalidOperationException("Research lease could not be renewed; execution stopped conservatively.");

                var chunk = analyzer.RunChunk(state, chunkSize, cancellationToken);
                state = chunk.State;
                if (jobs is not null)
                {
                    var serialized = DeterministicMonteCarloAnalyzer.SerializeState(state);
                    var progressHash = Hash($"{request.JobId}|MONTE_CARLO_BOOTSTRAP|{state.Cursor}|{state.TotalIterations}|{dataset.Fingerprint.ContentHash}|{configHash}|{engineVersion}|{serialized}");
                    await jobs.SaveProgressAsync(new ResearchProgress(request.JobId, "MONTE_CARLO_BOOTSTRAP", state.Cursor, state.TotalIterations,
                        dataset.Fingerprint.ContentHash, configHash, engineVersion, serialized, progressHash, DateTimeOffset.UtcNow), cancellationToken);
                    await jobs.SaveCheckpointAsync(new ResearchCheckpoint(request.JobId, state.Cursor, state.Cursor, dataset.Fingerprint.ContentHash, configHash, engineVersion, progressHash, DateTimeOffset.UtcNow), cancellationToken);
                }
            }

            var report = analyzer.FinalizeReport(request.JobId, state, "OHLCV bar-driven source backtest; resampling cannot create tick ordering, bid/ask, spread, or intrabar execution fidelity.");
            var evidenceHash = Hash($"{report.ReportFingerprint}|{dataset.Fingerprint.ContentHash}|{configHash}");
            await _evidence.AppendEvidenceAsync($"montecarlo:{request.JobId}", evidenceHash, cancellationToken);
            await _evidence.AppendEventAsync($"event:montecarlo:{request.JobId}", request.JobId, "MONTE_CARLO_COMPLETED", evidenceHash, cancellationToken);
            if (jobs is not null && job.State == DurableJobState.Running) await jobs.SaveJobAsync(ResearchJobLifecycle.Complete(job, DateTimeOffset.UtcNow), cancellationToken);
            return report;
        }
        catch (OperationCanceledException)
        {
            if (_evidence is IResearchJobStore jobs)
            {
                var existing = await jobs.LoadJobAsync(request.JobId, CancellationToken.None);
                if (existing is not null && existing.State is not DurableJobState.Completed and not DurableJobState.Canceled)
                    await jobs.SaveJobAsync(ResearchJobLifecycle.Cancel(existing, DateTimeOffset.UtcNow), CancellationToken.None);
            }
            throw;
        }
        finally { await _leases.ReleaseAsync(request.JobId, request.DeviceId, CancellationToken.None); }
    }


    public async Task<OptimizationReport> RunOptimizationAsync(
        ResearchRunRequest request, ParameterRange fastRange, ParameterRange slowRange,
        decimal pointValue = 1m, decimal commissionPoints = 0m, int chunkSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (chunkSize is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (!await _leases.TryAcquireAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
            throw new InvalidOperationException("Research job is already leased by another device or worker.");
        try
        {
            var dataset = await LoadAndGateAsync(request, cancellationToken);
            var engineVersion = "qf-native-v20.10";
            var configHash = Hash($"OPTIMIZATION|{request.StrategyId}|{request.Instrument}|{request.Timeframe}|{fastRange}|{slowRange}|{pointValue}|{commissionPoints}");
            var jobs = _evidence as IResearchJobStore;
            var existing = jobs is null ? null : await jobs.LoadJobAsync(request.JobId, cancellationToken);
            ValidateExisting(existing, dataset.Fingerprint.ContentHash, configHash, engineVersion);
            var cases = BuildOptimizationCases(request.JobId, dataset.Fingerprint.ContentHash, engineVersion, fastRange, slowRange, pointValue, commissionPoints);
            var state = await LoadOrCreateBatchStateAsync(jobs, request.JobId, "OPTIMIZATION", cases.Count, dataset.Fingerprint.ContentHash, configHash, engineVersion, cancellationToken);
            var results = JsonSerializer.Deserialize<List<OptimizationCaseResult>>(state.AggregationState) ?? new();
            var job = PrepareJob(existing, request.JobId, dataset.Fingerprint.ContentHash, configHash, engineVersion);
            if (jobs is not null) await jobs.SaveJobAsync(job, cancellationToken);

            while (state.Cursor < cases.Count)
            {
                await EnsureCanContinueAsync(jobs, request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken);
                var take = Math.Min(chunkSize, cases.Count - state.Cursor);
                var chunk = new DeterministicOptimizer().RunChunk(cases, dataset.Bars, request.Instrument, request.Timeframe, engineVersion,
                    pointValue, commissionPoints, state.Cursor, take);
                results.AddRange(chunk);
                state = await SaveBatchStateAsync(jobs, request.JobId, "OPTIMIZATION", state.Cursor + chunk.Count, cases.Count,
                    dataset.Fingerprint.ContentHash, configHash, engineVersion, JsonSerializer.Serialize(results), cancellationToken);
            }

            var searchFingerprint = Hash(string.Join("|", results.Select(x => x.Case.ConfigurationFingerprint + ":" + x.ResultHash)));
            var report = new OptimizationReport(request.JobId, results, searchFingerprint,
                "OHLCV bar-driven simulation; optimization does not provide tick ordering, bid/ask, spread, or intrabar execution fidelity.");
            await CompleteBatchAsync(jobs, job, request.JobId, "OPTIMIZATION", report.SearchFingerprint, dataset.Fingerprint.ContentHash, configHash, cancellationToken);
            return report;
        }
        catch (OperationCanceledException) { await MarkCanceledAsync(request.JobId); throw; }
        finally { await _leases.ReleaseAsync(request.JobId, request.DeviceId, CancellationToken.None); }
    }

    public async Task<RobustnessReport> RunRobustnessResumableAsync(
        ResearchRunRequest request, int baselineFast, int baselineSlow, int radius,
        decimal pointValue = 1m, decimal commissionPoints = 0m, int chunkSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (chunkSize is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (!await _leases.TryAcquireAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
            throw new InvalidOperationException("Research job is already leased by another device or worker.");
        try
        {
            var dataset = await LoadAndGateAsync(request, cancellationToken);
            var engineVersion = "qf-native-v20.10";
            var configHash = Hash($"ROBUSTNESS|{request.StrategyId}|{request.Instrument}|{request.Timeframe}|{baselineFast}|{baselineSlow}|{radius}|{pointValue}|{commissionPoints}");
            var jobs = _evidence as IResearchJobStore;
            var existing = jobs is null ? null : await jobs.LoadJobAsync(request.JobId, cancellationToken);
            ValidateExisting(existing, dataset.Fingerprint.ContentHash, configHash, engineVersion);
            var cases = BuildRobustnessCases(baselineFast, baselineSlow, radius);
            var baselineRequest = new BacktestRequest("EMA_CROSS", request.Instrument, request.Timeframe, dataset.Fingerprint.ContentHash, engineVersion);
            var baseline = new EmaCrossBacktestEngine().Run(dataset.Bars, baselineRequest, baselineFast, baselineSlow, pointValue, commissionPoints);
            var state = await LoadOrCreateBatchStateAsync(jobs, request.JobId, "ROBUSTNESS", cases.Count, dataset.Fingerprint.ContentHash, configHash, engineVersion, cancellationToken);
            var results = JsonSerializer.Deserialize<List<RobustnessCaseResult>>(state.AggregationState) ?? new();
            var job = PrepareJob(existing, request.JobId, dataset.Fingerprint.ContentHash, configHash, engineVersion);
            if (jobs is not null) await jobs.SaveJobAsync(job, cancellationToken);
            while (state.Cursor < cases.Count)
            {
                await EnsureCanContinueAsync(jobs, request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken);
                var take = Math.Min(chunkSize, cases.Count - state.Cursor);
                for (var i = state.Cursor; i < state.Cursor + take; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var c = cases[i];
                    var result = new EmaCrossBacktestEngine().Run(dataset.Bars, baselineRequest, c.FastPeriod, c.SlowPeriod, pointValue, commissionPoints);
                    results.Add(new RobustnessCaseResult(c, result.Metrics, result.Result.ResultHash));
                }
                state = await SaveBatchStateAsync(jobs, request.JobId, "ROBUSTNESS", state.Cursor + take, cases.Count,
                    dataset.Fingerprint.ContentHash, configHash, engineVersion, JsonSerializer.Serialize(results), cancellationToken);
            }
            var pnlRetention = baseline.Metrics.NetPnlPoints == 0m ? 0m : results.Count == 0 ? 0m : results.Average(x => x.Metrics.NetPnlPoints) / baseline.Metrics.NetPnlPoints;
            var avgDd = results.Count == 0 ? 0m : results.Average(x => x.Metrics.MaxDrawdownPoints);
            var ddRetention = baseline.Metrics.MaxDrawdownPoints == 0m ? 0m : baseline.Metrics.MaxDrawdownPoints / Math.Max(avgDd, 0.000001m);
            var stability = Hash(string.Join("|", results.Select(x => $"{x.Perturbation.FastPeriod}:{x.Perturbation.SlowPeriod}:{x.ResultHash}")));
            var report = new RobustnessReport(request.JobId, baseline.Result.ResultHash, results, pnlRetention, ddRetention, stability,
                "OHLCV bar-driven perturbation analysis; it does not create tick ordering, bid/ask, spread, or intrabar execution fidelity.");
            await CompleteBatchAsync(jobs, job, request.JobId, "ROBUSTNESS", stability, dataset.Fingerprint.ContentHash, configHash, cancellationToken);
            return report;
        }
        catch (OperationCanceledException) { await MarkCanceledAsync(request.JobId); throw; }
        finally { await _leases.ReleaseAsync(request.JobId, request.DeviceId, CancellationToken.None); }
    }

    public async Task<WalkForwardReport> RunWalkForwardResumableAsync(
        ResearchRunRequest request, int trainBars, int testBars, int stepBars,
        ParameterRange fastRange, ParameterRange slowRange, decimal pointValue = 1m,
        decimal commissionPoints = 0m, CancellationToken cancellationToken = default)
    {
        if (!await _leases.TryAcquireAsync(request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken))
            throw new InvalidOperationException("Research job is already leased by another device or worker.");
        try
        {
            var dataset = await LoadAndGateAsync(request, cancellationToken);
            var engineVersion = "qf-native-v20.10";
            var configHash = Hash($"WALK_FORWARD|{request.StrategyId}|{request.Instrument}|{request.Timeframe}|{trainBars}|{testBars}|{stepBars}|{fastRange}|{slowRange}|{pointValue}|{commissionPoints}");
            var jobs = _evidence as IResearchJobStore;
            var existing = jobs is null ? null : await jobs.LoadJobAsync(request.JobId, cancellationToken);
            ValidateExisting(existing, dataset.Fingerprint.ContentHash, configHash, engineVersion);
            var windows = BuildWalkWindows(dataset.Bars.Count, trainBars, testBars, stepBars);
            var state = await LoadOrCreateBatchStateAsync(jobs, request.JobId, "WALK_FORWARD", windows.Count, dataset.Fingerprint.ContentHash, configHash, engineVersion, cancellationToken);
            var results = JsonSerializer.Deserialize<List<WalkForwardCaseResult>>(state.AggregationState) ?? new();
            var job = PrepareJob(existing, request.JobId, dataset.Fingerprint.ContentHash, configHash, engineVersion);
            if (jobs is not null) await jobs.SaveJobAsync(job, cancellationToken);
            while (state.Cursor < windows.Count)
            {
                await EnsureCanContinueAsync(jobs, request.JobId, request.DeviceId, request.LeaseDuration, cancellationToken);
                var w = windows[state.Cursor];
                var train = dataset.Bars.Skip(w.TrainStart).Take(trainBars).ToList();
                var test = dataset.Bars.Skip(w.TestStart).Take(testBars).ToList();
                var trainFingerprint = dataset.Fingerprint.ContentHash + $":train:{w.TrainStart}:{trainBars}";
                var trainReport = new DeterministicOptimizer().Run($"{request.JobId}:train:{w.WindowId}", train, trainFingerprint, request.Instrument, request.Timeframe, engineVersion, fastRange, slowRange, pointValue, commissionPoints);
                var selected = trainReport.Cases.OrderByDescending(x => x.Metrics.NetPnlPoints).ThenBy(x => x.Metrics.MaxDrawdownPoints).ThenBy(x => x.Case.FastPeriod).ThenBy(x => x.Case.SlowPeriod).First();
                var testRequest = new BacktestRequest("EMA_CROSS", request.Instrument, request.Timeframe,
                    dataset.Fingerprint.ContentHash + $":test:{w.TestStart}:{testBars}", engineVersion);
                var oos = new EmaCrossBacktestEngine().Run(test, testRequest, selected.Case.FastPeriod, selected.Case.SlowPeriod, pointValue, commissionPoints);
                results.Add(new WalkForwardCaseResult(w, selected.Case.FastPeriod, selected.Case.SlowPeriod, selected.Metrics, oos.Metrics));
                state = await SaveBatchStateAsync(jobs, request.JobId, "WALK_FORWARD", state.Cursor + 1, windows.Count,
                    dataset.Fingerprint.ContentHash, configHash, engineVersion, JsonSerializer.Serialize(results), cancellationToken);
            }
            var fingerprint = Hash(string.Join("|", results.Select(x => $"{x.Window.WindowId}:{x.FastPeriod}:{x.SlowPeriod}:{x.Training.NetPnlPoints}:{x.OutOfSample.NetPnlPoints}")));
            var report = new WalkForwardReport(request.JobId, results, fingerprint,
                "Chronological train/test evaluation over OHLCV bar data; parameter selection is isolated to each training window and is not performed on the following test window.");
            await CompleteBatchAsync(jobs, job, request.JobId, "WALK_FORWARD", fingerprint, dataset.Fingerprint.ContentHash, configHash, cancellationToken);
            return report;
        }
        catch (OperationCanceledException) { await MarkCanceledAsync(request.JobId); throw; }
        finally { await _leases.ReleaseAsync(request.JobId, request.DeviceId, CancellationToken.None); }
    }

    private async Task<ImportedMarketDataset> LoadAndGateAsync(ResearchRunRequest request, CancellationToken cancellationToken)
    {
        var dataset = await _datasets.LoadDatasetAsync(request.DatasetId, cancellationToken)
            ?? throw new InvalidOperationException($"Dataset '{request.DatasetId}' was not found in local storage.");
        var gate = _dataGate.Validate(dataset);
        if (!gate.Allowed) throw new InvalidOperationException("Canonical data gate blocked research execution: " + string.Join(",", gate.Errors));
        if (!string.Equals(dataset.Instrument, request.Instrument, StringComparison.Ordinal) || !string.Equals(dataset.Timeframe, request.Timeframe, StringComparison.Ordinal))
            throw new InvalidOperationException("Research request does not match the stored dataset identity.");
        return dataset;
    }

    private static void ValidateExisting(ResearchJob? existing, string datasetFingerprint, string configHash, string engineVersion)
    {
        if (existing is not null && (existing.DatasetFingerprint != datasetFingerprint || existing.ConfigurationFingerprint != configHash || existing.EngineFingerprint != engineVersion))
            throw new InvalidOperationException("Existing research job fingerprint does not match the requested configuration.");
    }

    private static ResearchJob PrepareJob(ResearchJob? existing, string jobId, string datasetFingerprint, string configHash, string engineVersion)
    {
        var now = DateTimeOffset.UtcNow;
        var job = existing ?? ResearchJobLifecycle.Create(jobId, datasetFingerprint, configHash, engineVersion, now);
        return job.State is DurableJobState.Queued or DurableJobState.Paused or DurableJobState.RecoveryRequired ? ResearchJobLifecycle.Start(job, now) : job;
    }

    private static List<OptimizationCase> BuildOptimizationCases(string jobId, string datasetFingerprint, string engineVersion, ParameterRange fastRange, ParameterRange slowRange, decimal pointValue, decimal commissionPoints)
    {
        ArgumentNullException.ThrowIfNull(fastRange);
        ArgumentNullException.ThrowIfNull(slowRange);
        if (fastRange.Min <= 0 || fastRange.Max < fastRange.Min || fastRange.Step <= 0 || slowRange.Min <= 0 || slowRange.Max < slowRange.Min || slowRange.Step <= 0)
            throw new ArgumentException("Invalid optimization parameter range.");
        if (fastRange.Min >= slowRange.Max) throw new ArgumentException("Parameter ranges must contain at least one valid fast/slow combination.");
        var list = new List<OptimizationCase>();
        for (var fast = fastRange.Min; fast <= fastRange.Max; fast += fastRange.Step)
        for (var slow = slowRange.Min; slow <= slowRange.Max; slow += slowRange.Step)
            if (fast < slow)
                list.Add(new OptimizationCase($"{jobId}:{fast}:{slow}", fast, slow, datasetFingerprint, Hash($"EMA_CROSS|fast={fast}|slow={slow}|point={pointValue}|commission={commissionPoints}|engine={engineVersion}")));
        if (list.Count == 0) throw new ArgumentException("Parameter ranges contain no valid fast/slow combinations.");
        return list;
    }

    private static List<ParameterPerturbation> BuildRobustnessCases(int baselineFast, int baselineSlow, int radius)
    {
        if (radius < 0 || radius > 10 || baselineFast <= 0 || baselineSlow <= baselineFast) throw new ArgumentException("Invalid robustness parameters.");
        var list = new List<ParameterPerturbation>();
        for (var fast = Math.Max(1, baselineFast - radius); fast <= baselineFast + radius; fast++)
        for (var slow = Math.Max(fast + 1, baselineSlow - radius); slow <= baselineSlow + radius; slow++)
            list.Add(new ParameterPerturbation($"fast={fast},slow={slow}", fast, slow));
        return list;
    }

    private static List<WalkForwardWindow> BuildWalkWindows(int barCount, int trainBars, int testBars, int stepBars)
    {
        if (trainBars <= 0 || testBars <= 0 || stepBars <= 0 || barCount < trainBars + testBars) throw new ArgumentException("Invalid walk-forward window configuration.");
        var list = new List<WalkForwardWindow>(); var id = 0;
        for (var start = 0; start + trainBars + testBars <= barCount; start += stepBars)
            list.Add(new WalkForwardWindow(id++, start, start + trainBars, start + trainBars, start + trainBars + testBars));
        return list;
    }

    private static async Task<ResearchProgress> LoadOrCreateBatchStateAsync(IResearchJobStore? jobs, string jobId, string operation, int total, string datasetFingerprint, string configHash, string engineVersion, CancellationToken cancellationToken)
    {
        if (jobs is null) return new ResearchProgress(jobId, operation, 0, total, datasetFingerprint, configHash, engineVersion, "[]", Hash($"{jobId}|{operation}|0|{total}|{datasetFingerprint}|{configHash}|{engineVersion}|[]"), DateTimeOffset.UtcNow);
        var progress = await jobs.LoadProgressAsync(jobId, operation, cancellationToken);
        if (progress is null) return new ResearchProgress(jobId, operation, 0, total, datasetFingerprint, configHash, engineVersion, "[]", Hash($"{jobId}|{operation}|0|{total}|{datasetFingerprint}|{configHash}|{engineVersion}|[]"), DateTimeOffset.UtcNow);
        if (progress.Total != total || progress.Cursor > total || progress.DatasetFingerprint != datasetFingerprint || progress.ConfigurationFingerprint != configHash || progress.EngineFingerprint != engineVersion)
            throw new InvalidOperationException($"Stored {operation} progress cannot be resumed because its fingerprint or total differs.");
        var expected = Hash($"{progress.JobId}|{progress.Operation}|{progress.Cursor}|{progress.Total}|{progress.DatasetFingerprint}|{progress.ConfigurationFingerprint}|{progress.EngineFingerprint}|{progress.AggregationState}");
        if (expected != progress.ProgressHash) throw new InvalidOperationException($"Stored {operation} progress failed integrity verification.");
        return progress;
    }

    private static async Task<ResearchProgress> SaveBatchStateAsync(IResearchJobStore? jobs, string jobId, string operation, int cursor, int total, string datasetFingerprint, string configHash, string engineVersion, string aggregationState, CancellationToken cancellationToken)
    {
        var hash = Hash($"{jobId}|{operation}|{cursor}|{total}|{datasetFingerprint}|{configHash}|{engineVersion}|{aggregationState}");
        var progress = new ResearchProgress(jobId, operation, cursor, total, datasetFingerprint, configHash, engineVersion, aggregationState, hash, DateTimeOffset.UtcNow);
        if (jobs is not null)
        {
            await jobs.SaveProgressAsync(progress, cancellationToken);
            await jobs.SaveCheckpointAsync(new ResearchCheckpoint(jobId, cursor, cursor, datasetFingerprint, configHash, engineVersion, hash, DateTimeOffset.UtcNow), cancellationToken);
        }
        return progress;
    }

    private async Task EnsureCanContinueAsync(IResearchJobStore? jobs, string jobId, string deviceId, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cancellation = jobs is null ? null : await jobs.LoadCancellationAsync(jobId, cancellationToken);
        if (cancellation is not null) throw new OperationCanceledException($"Research job canceled: {cancellation.Reason}", cancellationToken);
        if (!await _leases.RenewAsync(jobId, deviceId, leaseDuration, cancellationToken)) throw new InvalidOperationException("Research lease could not be renewed; execution stopped conservatively.");
    }

    private async Task CompleteBatchAsync(IResearchJobStore? jobs, ResearchJob job, string jobId, string operation, string reportFingerprint, string datasetFingerprint, string configHash, CancellationToken cancellationToken)
    {
        var evidenceHash = Hash($"{operation}|{reportFingerprint}|{datasetFingerprint}|{configHash}");
        var evidenceId = $"{operation.ToLowerInvariant()}:{jobId}";
        if (!await _evidence.ContainsEvidenceAsync(evidenceId, cancellationToken))
            await _evidence.AppendEvidenceAsync(evidenceId, evidenceHash, cancellationToken);
        await _evidence.AppendEventAsync($"event:{operation.ToLowerInvariant()}:{jobId}", jobId, $"{operation}_COMPLETED", evidenceHash, cancellationToken);
        if (jobs is not null && job.State == DurableJobState.Running) await jobs.SaveJobAsync(ResearchJobLifecycle.Complete(job, DateTimeOffset.UtcNow), cancellationToken);
    }

    private async Task MarkCanceledAsync(string jobId)
    {
        if (_evidence is IResearchJobStore jobs)
        {
            var existing = await jobs.LoadJobAsync(jobId, CancellationToken.None);
            if (existing is not null && existing.State is not DurableJobState.Completed and not DurableJobState.Canceled)
                await jobs.SaveJobAsync(ResearchJobLifecycle.Cancel(existing, DateTimeOffset.UtcNow), CancellationToken.None);
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
