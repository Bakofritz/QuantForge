using QuantForge.Core;

namespace QuantForge.Storage;

/// <summary>
/// Test-only adapter that connects the storage fault injector to the real SQLite store.
/// It deliberately exercises one real storage operation per configured fault point.
/// It never enables application-level parallel research execution.
/// </summary>
public sealed class SqliteStorageFaultExecutionAdapter
{
    public async Task<StorageFaultInjectionResult> RunAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var checks = new List<StorageFaultInjectionCheck>();
        foreach (var point in StandardStorageFaultPoints())
        {
            cancellationToken.ThrowIfCancellationRequested();
            checks.Add(await ProbeAsync(databasePath, point, cancellationToken));
        }

        var passed = checks.All(c => c.Disposition == StorageFaultDisposition.ObservedAndContained);
        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks.Select(c => $"{c.Point}:{c.Disposition}:{c.Detail}")));
        return new StorageFaultInjectionResult(
            "QF-SQLITE-FAULT-EXECUTION-1",
            passed,
            checks.AsReadOnly(),
            fingerprint,
            true,
            true);
    }

    private static async Task<StorageFaultInjectionCheck> ProbeAsync(string databasePath, StorageFaultPoint point, CancellationToken cancellationToken)
    {
        var injector = new StorageFaultInjector(new StorageFaultPlan(point));
        await using var store = new SqliteLocalStore(databasePath, injector);
        await store.InitializeAsync(cancellationToken);

        try
        {
            await ExecutePointAsync(store, point, cancellationToken);
            return new(point, StorageFaultDisposition.UnexpectedlySucceeded, "The real SQLite operation completed even though the configured test fault was expected.");
        }
        catch (StorageFaultInjectedException ex) when (ex.Point == point && ex.Invocation == 1)
        {
            return new(point, StorageFaultDisposition.ObservedAndContained, $"Real SQLite operation surfaced the injected fault as {ex.ErrorCode}.");
        }
        catch (Exception ex)
        {
            return new(point, StorageFaultDisposition.UnexpectedException, ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static async Task ExecutePointAsync(SqliteLocalStore store, StorageFaultPoint point, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        switch (point)
        {
            case StorageFaultPoint.LeaseAcquire:
                await store.TryAcquireAsync("fault-lease-job", "fault-worker", TimeSpan.FromMinutes(1), cancellationToken);
                break;
            case StorageFaultPoint.LeaseRenew:
                await store.TryAcquireAsync("fault-renew-job", "fault-worker", TimeSpan.FromMinutes(1), cancellationToken);
                // The injector is configured only for renewal, so the acquisition above is allowed.
                await store.RenewAsync("fault-renew-job", "fault-worker", TimeSpan.FromMinutes(1), cancellationToken);
                break;
            case StorageFaultPoint.CheckpointWrite:
                await store.SaveCheckpointAsync(new ResearchCheckpoint("fault-checkpoint-job", 1, 1, "dataset", "config", "engine", "state", now), cancellationToken);
                break;
            case StorageFaultPoint.ReceiptWrite:
                await store.AppendExecutionReceiptAsync(new ResourceReceiptRecord("fault-receipt", "batch", "context", "job", "worker", "worker-fingerprint", "Completed", "result", now, now, 1, 0, "OK", null), cancellationToken);
                break;
            case StorageFaultPoint.EvidenceWrite:
                await store.AppendEvidenceAsync("fault-evidence", "fault-hash", cancellationToken);
                break;
            case StorageFaultPoint.ValidationResultWrite:
                var validation = new StorageConcurrencyValidationResult("QF-FAULT-PROBE", true, Array.Empty<StorageConcurrencyValidationCheck>(), ResearchFingerprint.Sha256("fault-validation"), true);
                await store.AppendConcurrencyValidationResultAsync(validation, cancellationToken);
                break;
            case StorageFaultPoint.JobStateWrite:
                await store.SaveJobAsync(new ResearchJob("fault-job", DurableJobState.Running, "dataset", "config", "engine", now, now), cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(point));
        }
    }
    /// <summary>
    /// Executes each native SQLite fault point, then retries the same logical operation without
    /// fault injection and verifies that durable state is either absent after the failed write or
    /// correctly established by the subsequent controlled retry. This is a validation-only path.
    /// </summary>
    public async Task<StorageFaultRecoveryValidationResult> RunRecoveryInvariantAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var checks = new List<StorageFaultRecoveryCheck>();
        foreach (var point in StandardStorageFaultPoints())
        {
            cancellationToken.ThrowIfCancellationRequested();
            checks.Add(await ProbeRecoveryInvariantAsync(databasePath, point, cancellationToken));
        }

        var passed = checks.All(c => c.FaultContained && c.RetrySucceeded && c.DurableStateVerified);
        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks.Select(c =>
            $"{c.Point}:{c.FaultContained}:{c.RetrySucceeded}:{c.DurableStateVerified}:{c.Detail}")));
        return new StorageFaultRecoveryValidationResult(
            "QF-SQLITE-FAULT-RECOVERY-1",
            passed,
            checks.AsReadOnly(),
            fingerprint,
            true,
            true);
    }

    private static async Task<StorageFaultRecoveryCheck> ProbeRecoveryInvariantAsync(string databasePath, StorageFaultPoint point, CancellationToken cancellationToken)
    {
        var suffix = point.ToString().ToLowerInvariant();
        var injector = new StorageFaultInjector(new StorageFaultPlan(point));
        await using var faulted = new SqliteLocalStore(databasePath, injector);
        await faulted.InitializeAsync(cancellationToken);
        var faultContained = false;
        try
        {
            await ExecutePointAsync(faulted, point, cancellationToken);
        }
        catch (StorageFaultInjectedException ex) when (ex.Point == point && ex.Invocation == 1)
        {
            faultContained = true;
        }
        catch (Exception ex)
        {
            return new(point, false, false, false, "Unexpected fault-path exception: " + ex.GetType().Name + ": " + ex.Message);
        }

        if (!faultContained)
            return new(point, false, false, false, "Configured native storage fault was not observed.");

        await using var retry = new SqliteLocalStore(databasePath);
        await retry.InitializeAsync(cancellationToken);
        try
        {
            await RetryPointAsync(retry, point, suffix, cancellationToken);
            var verified = await VerifyRetryStateAsync(retry, point, suffix, cancellationToken);
            return new(point, true, true, verified, verified ? "Fault was contained and controlled retry established verifiable durable state." : "Retry completed but durable-state verification failed.");
        }
        catch (Exception ex)
        {
            return new(point, true, false, false, "Controlled retry failed: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static async Task RetryPointAsync(SqliteLocalStore store, StorageFaultPoint point, string suffix, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        switch (point)
        {
            case StorageFaultPoint.LeaseAcquire:
                if (!await store.TryAcquireAsync($"recovery-{suffix}-lease", "recovery-worker", TimeSpan.FromMinutes(1), cancellationToken))
                    throw new InvalidOperationException("LEASE_RETRY_NOT_ACQUIRED");
                break;
            case StorageFaultPoint.LeaseRenew:
                if (!await store.RenewAsync("fault-renew-job", "fault-worker", TimeSpan.FromMinutes(1), cancellationToken))
                    throw new InvalidOperationException("LEASE_RETRY_NOT_RENEWED");
                break;
            case StorageFaultPoint.CheckpointWrite:
                await store.SaveCheckpointAsync(new ResearchCheckpoint("fault-checkpoint-job", 1, 1, "dataset", "config", "engine", "state", now), cancellationToken);
                break;
            case StorageFaultPoint.ReceiptWrite:
                await store.AppendExecutionReceiptAsync(new ResourceReceiptRecord("fault-receipt", "batch", "context", "job", "worker", "worker-fingerprint", "Completed", "result", now, now, 1, 0, "OK", null), cancellationToken);
                break;
            case StorageFaultPoint.EvidenceWrite:
                await store.AppendEvidenceAsync("fault-evidence", "fault-hash", cancellationToken);
                break;
            case StorageFaultPoint.ValidationResultWrite:
                var validation = new StorageConcurrencyValidationResult("QF-FAULT-PROBE", true, Array.Empty<StorageConcurrencyValidationCheck>(), ResearchFingerprint.Sha256("fault-validation"), true);
                await store.AppendConcurrencyValidationResultAsync(validation, cancellationToken);
                break;
            case StorageFaultPoint.JobStateWrite:
                await store.SaveJobAsync(new ResearchJob("fault-job", DurableJobState.Running, "dataset", "config", "engine", now, now), cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(point));
        }
    }

    private static async Task<bool> VerifyRetryStateAsync(SqliteLocalStore store, StorageFaultPoint point, string suffix, CancellationToken cancellationToken)
    {
        switch (point)
        {
            case StorageFaultPoint.LeaseAcquire:
                return await store.LoadLeaseAsync($"recovery-{suffix}-lease", cancellationToken) is not null;
            case StorageFaultPoint.LeaseRenew:
                var lease = await store.LoadLeaseAsync("fault-renew-job", cancellationToken);
                return lease is not null && lease.Version >= 2;
            case StorageFaultPoint.CheckpointWrite:
                return await store.LoadLatestCheckpointAsync("fault-checkpoint-job", cancellationToken) is { Sequence: 1, Cursor: 1, StateHash: "state" };
            case StorageFaultPoint.ReceiptWrite:
                return (await store.LoadExecutionReceiptsForJobAsync("job", cancellationToken)).Any(r => r.ReceiptFingerprint == "fault-receipt");
            case StorageFaultPoint.EvidenceWrite:
                return await store.ContainsEvidenceAsync("fault-evidence", cancellationToken);
            case StorageFaultPoint.ValidationResultWrite:
                return await store.LoadConcurrencyValidationResultAsync(ResearchFingerprint.Sha256("fault-validation"), cancellationToken) is not null;
            case StorageFaultPoint.JobStateWrite:
                return await store.LoadJobAsync("fault-job", cancellationToken) is { State: DurableJobState.Running };
            default:
                return false;
        }
    }

    private static StorageFaultPoint[] StandardStorageFaultPoints() => new[]
    {
        StorageFaultPoint.LeaseAcquire, StorageFaultPoint.LeaseRenew, StorageFaultPoint.CheckpointWrite,
        StorageFaultPoint.ReceiptWrite, StorageFaultPoint.EvidenceWrite, StorageFaultPoint.ValidationResultWrite,
        StorageFaultPoint.JobStateWrite
    };

}


public sealed record RecoveryBindingReconciliationNativeFaultCheck(
    StorageFaultPoint Point,
    bool FaultContained,
    bool RolledBack,
    bool RetrySucceeded,
    bool Idempotent,
    bool ConflictRejected,
    string Detail);

public sealed record RecoveryBindingReconciliationNativeFaultValidationResult(
    string HarnessVersion,
    bool Passed,
    IReadOnlyList<RecoveryBindingReconciliationNativeFaultCheck> Checks,
    string ResultFingerprint,
    bool FailClosedExpected,
    bool NativeStorageExecuted);

public sealed class RecoveryBindingReconciliationNativeFaultAdapter
{
    public async Task<RecoveryBindingReconciliationNativeFaultValidationResult> RunAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var checks = new List<RecoveryBindingReconciliationNativeFaultCheck>();
        foreach (var point in new[] { StorageFaultPoint.RecoveryAuditWrite, StorageFaultPoint.EvidenceWrite, StorageFaultPoint.TerminalEventWrite })
        {
            cancellationToken.ThrowIfCancellationRequested();
            checks.Add(await ProbeAsync(databasePath, point, cancellationToken));
        }
        var passed = checks.All(c => c.FaultContained && c.RolledBack && c.RetrySucceeded && c.Idempotent && c.ConflictRejected);
        var fingerprint = QuantForge.Core.ResearchFingerprint.Sha256(string.Join("|", checks.Select(c => $"{c.Point}:{c.FaultContained}:{c.RolledBack}:{c.RetrySucceeded}:{c.Idempotent}:{c.ConflictRejected}:{c.Detail}")));
        return new("QF-SQLITE-RECOVERY-RECONCILIATION-FAULT-1", passed, checks.AsReadOnly(), fingerprint, true, true);
    }

    private static async Task<RecoveryBindingReconciliationNativeFaultCheck> ProbeAsync(string databasePath, StorageFaultPoint point, CancellationToken cancellationToken)
    {
        var binding = BuildBinding(point.ToString());
        var faulted = new StorageFaultInjector(new StorageFaultPlan(point));
        var faultContained = false;
        try
        {
            await using var store = new SqliteLocalStore(databasePath, faulted);
            await store.InitializeAsync(cancellationToken);
            await store.AppendRecoveryBindingReconciliationAtomicallyAsync(binding.audit, binding.evidenceId, binding.evidenceHash, binding.eventId, binding.eventJobId, binding.eventType, binding.eventPayloadHash, cancellationToken);
        }
        catch (StorageFaultInjectedException ex) when (ex.Point == point && ex.Invocation == 1) { faultContained = true; }
        catch (Exception ex) { return new(point, false, false, false, false, false, "Unexpected fault-path exception: " + ex.GetType().Name + ": " + ex.Message); }

        if (!faultContained) return new(point, false, false, false, false, false, "Configured native SQLite reconciliation fault was not observed.");

        await using var afterFault = new SqliteLocalStore(databasePath);
        await afterFault.InitializeAsync(cancellationToken);
        var rolledBack = !await afterFault.ContainsRecoveryAuditAsync(binding.audit.AuditFingerprint, cancellationToken)
            && !await afterFault.ContainsEvidenceAsync(binding.evidenceId, cancellationToken)
            && !await afterFault.ContainsResearchEventAsync(binding.eventId, binding.eventPayloadHash, cancellationToken);
        if (!rolledBack)
            return new(point, true, false, false, false, false, "Injected failure left one or more reconciliation records durable; atomic rollback invariant failed.");

        await using var retry = new SqliteLocalStore(databasePath);
        await retry.InitializeAsync(cancellationToken);
        try
        {
            await retry.AppendRecoveryBindingReconciliationAtomicallyAsync(binding.audit, binding.evidenceId, binding.evidenceHash, binding.eventId, binding.eventJobId, binding.eventType, binding.eventPayloadHash, cancellationToken);
            var complete = await VerifyCompleteAsync(retry, binding, cancellationToken);
            if (!complete) return new(point, true, true, true, false, false, "Retry completed but the three durable reconciliation records were not all present.");
            await retry.AppendRecoveryBindingReconciliationAtomicallyAsync(binding.audit, binding.evidenceId, binding.evidenceHash, binding.eventId, binding.eventJobId, binding.eventType, binding.eventPayloadHash, cancellationToken);
            var idempotent = await VerifyCompleteAsync(retry, binding, cancellationToken);
            var conflictRejected = await VerifyConflictRejectedAsync(retry, binding, cancellationToken);
            return new(point, true, rolledBack, true, idempotent, conflictRejected, "Native SQLite transaction rolled back the injected failure, then accepted a clean retry, an identical retry, and rejected a conflicting identity.");
        }
        catch (Exception ex) { return new(point, true, true, false, false, false, "Controlled retry failed: " + ex.GetType().Name + ": " + ex.Message); }
    }

    private static async Task<bool> VerifyCompleteAsync(SqliteLocalStore store, (RecoveryAuditRecord audit,string evidenceId,string evidenceHash,string eventId,string eventJobId,string eventType,string eventPayloadHash) b, CancellationToken ct)
    {
        if (!await store.ContainsEvidenceAsync(b.evidenceId, ct)) return false;
        // A successful atomic commit is also checked through the evidence/event identity on the same database.
        return await store.ContainsResearchEventAsync(b.eventId, b.eventPayloadHash, ct) && await store.ContainsRecoveryAuditAsync(b.audit.AuditFingerprint, ct);
    }

    private static async Task<bool> VerifyConflictRejectedAsync(SqliteLocalStore store, (RecoveryAuditRecord audit,string evidenceId,string evidenceHash,string eventId,string eventJobId,string eventType,string eventPayloadHash) b, CancellationToken ct)
    {
        var conflict = b.audit with { Reason = b.audit.Reason + "-CONFLICT" };
        try
        {
            await store.AppendRecoveryBindingReconciliationAtomicallyAsync(conflict, b.evidenceId, b.evidenceHash, b.eventId, b.eventJobId, b.eventType, b.eventPayloadHash, ct);
            return false;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase)) { return true; }
    }

    private static (RecoveryAuditRecord audit,string evidenceId,string evidenceHash,string eventId,string eventJobId,string eventType,string eventPayloadHash) BuildBinding(string suffix)
    {
        var audit = new RecoveryAuditRecord(
            QuantForge.Core.ResearchFingerprint.Sha256("native-recovery-audit|" + suffix),
            "native-batch", "native-context", "native-job", "native-worker",
            "RecoveryPreflightBinding", "BoundAndConsistent", "native reconciliation validation", null, DateTimeOffset.UtcNow);
        var evidenceHash = QuantForge.Core.ResearchFingerprint.Sha256("native-evidence|" + suffix);
        return (audit, "native-recovery-evidence-" + suffix, evidenceHash, "native-recovery-event-" + suffix, audit.JobId, "RECOVERY_BINDING_RECONCILIATION", evidenceHash);
    }
}
