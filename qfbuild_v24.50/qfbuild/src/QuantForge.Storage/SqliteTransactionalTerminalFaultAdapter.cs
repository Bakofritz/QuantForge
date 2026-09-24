using Microsoft.Data.Sqlite;
using QuantForge.Core;

namespace QuantForge.Storage;

/// <summary>
/// Validation-only harness for the SQLite terminal transaction. Each injected failure must
/// leave the terminal outcome absent, after which the same logical terminal commit must succeed.
/// </summary>
public sealed class SqliteTransactionalTerminalFaultAdapter
{
    public async Task<TransactionalTerminalFaultValidationResult> RunAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var checks = new List<TransactionalTerminalFaultCheck>();
        foreach (var point in Enum.GetValues<TransactionalTerminalFaultPoint>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            checks.Add(await ProbeAsync(databasePath, point, cancellationToken));
        }

        var passed = checks.All(c => c.FaultContained && c.RolledBack && c.RetryCommitted &&
                                     c.TerminalStateVerified && c.ReceiptVerified &&
                                     c.EvidenceVerified && c.EventVerified);
        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks.Select(c =>
            $"{c.Point}:{c.FaultContained}:{c.RolledBack}:{c.RetryCommitted}:{c.TerminalStateVerified}:{c.ReceiptVerified}:{c.EvidenceVerified}:{c.EventVerified}:{c.Detail}")));
        return new TransactionalTerminalFaultValidationResult(
            "QF-SQLITE-TRANSACTIONAL-TERMINAL-FAULT-1",
            passed,
            checks.AsReadOnly(),
            fingerprint,
            true,
            true);
    }

    private static async Task<TransactionalTerminalFaultCheck> ProbeAsync(
        string databasePath,
        TransactionalTerminalFaultPoint point,
        CancellationToken cancellationToken)
    {
        var jobId = $"tx-fault-{point.ToString().ToLowerInvariant()}";
        var workerId = "tx-fault-worker";
        var now = DateTimeOffset.UtcNow;
        var job = new ResearchJob(jobId, DurableJobState.Running, "dataset", "config", "engine", now, now);
        var receipt = new TerminalCommitReceipt(
            ResearchFingerprint.Sha256($"receipt|{jobId}"), "batch", "context", jobId, workerId,
            "worker-fingerprint", DurableJobState.Completed.ToString(), "result", now, now, 1, 0, "TEST", null);
        var evidenceId = $"tx-evidence:{jobId}";
        var evidenceHash = ResearchFingerprint.Sha256($"evidence|{jobId}");
        var eventId = $"tx-event:{jobId}";
        var request = new TerminalCommitRequest(
            job, receipt, evidenceId, evidenceHash, eventId,
            "TRANSACTIONAL_TERMINAL_FAULT_TEST", evidenceHash);

        await using (var seed = new SqliteLocalStore(databasePath))
        {
            await seed.InitializeAsync(cancellationToken);
            await seed.SaveJobAsync(job, cancellationToken);
            if (!await seed.TryAcquireAsync(jobId, workerId, TimeSpan.FromMinutes(5), cancellationToken))
                return Fail(point, "Unable to establish seed lease.");
        }

        var injected = MapFault(point);
        var faultContained = false;
        try
        {
            await using var faulted = new SqliteLocalStore(databasePath, new StorageFaultInjector(new StorageFaultPlan(injected)));
            await faulted.InitializeAsync(cancellationToken);
            await faulted.CommitTerminalOutcomeAsync(request with { Job = job with { State = DurableJobState.Completed, UpdatedAt = DateTimeOffset.UtcNow } }, cancellationToken);
        }
        catch (StorageFaultInjectedException ex) when (ex.Point == injected && ex.Invocation == 1)
        {
            faultContained = true;
        }
        catch (Exception ex)
        {
            return Fail(point, "Unexpected injected-path exception: " + ex.GetType().Name + ": " + ex.Message);
        }

        if (!faultContained)
            return Fail(point, "Configured transaction fault was not observed.");

        var rolledBack = await VerifyRollbackAsync(databasePath, request, cancellationToken);
        if (!rolledBack)
            return Fail(point, "Failed terminal transaction left durable terminal state behind.", faultContained: true);

        var retryCommitted = false;
        try
        {
            await using var retry = new SqliteLocalStore(databasePath);
            await retry.InitializeAsync(cancellationToken);
            await retry.CommitTerminalOutcomeAsync(request with { Job = job with { State = DurableJobState.Completed, UpdatedAt = DateTimeOffset.UtcNow } }, cancellationToken);
            retryCommitted = true;
        }
        catch (Exception ex)
        {
            return Fail(point, "Controlled retry failed: " + ex.GetType().Name + ": " + ex.Message, true, rolledBack);
        }

        var verified = await VerifyCommittedStateAsync(databasePath, request, cancellationToken);
        return new TransactionalTerminalFaultCheck(
            point, true, rolledBack, retryCommitted,
            verified.JobState, verified.Receipt, verified.Evidence, verified.Event,
            verified.JobState && verified.Receipt && verified.Evidence && verified.Event
                ? "Injected transaction failure rolled back completely; clean retry committed and all terminal artifacts were verified."
                : "Retry committed but one or more terminal artifacts could not be verified.");
    }

    private static StorageFaultPoint MapFault(TransactionalTerminalFaultPoint point) => point switch
    {
        TransactionalTerminalFaultPoint.JobStateWrite => StorageFaultPoint.JobStateWrite,
        TransactionalTerminalFaultPoint.ReceiptWrite => StorageFaultPoint.ReceiptWrite,
        TransactionalTerminalFaultPoint.EvidenceWrite => StorageFaultPoint.EvidenceWrite,
        TransactionalTerminalFaultPoint.EventWrite => StorageFaultPoint.TerminalEventWrite,
        TransactionalTerminalFaultPoint.BeforeCommit => StorageFaultPoint.TerminalCommitBeforeCommit,
        _ => throw new ArgumentOutOfRangeException(nameof(point))
    };

    private static async Task<bool> VerifyRollbackAsync(string databasePath, TerminalCommitRequest request, CancellationToken cancellationToken)
    {
        var state = await ReadScalarAsync(databasePath, "SELECT state FROM research_jobs WHERE job_id=$job;", request.Job.JobId, cancellationToken);
        var receipt = await ReadScalarAsync(databasePath, "SELECT receipt_fingerprint FROM resource_execution_receipts WHERE receipt_fingerprint=$id;", request.Receipt.ReceiptFingerprint, cancellationToken);
        var evidence = await ReadScalarAsync(databasePath, "SELECT evidence_id FROM evidence WHERE evidence_id=$id;", request.EvidenceId, cancellationToken);
        var evt = await ReadScalarAsync(databasePath, "SELECT event_id FROM research_events WHERE event_id=$id;", request.EventId, cancellationToken);
        return string.Equals(state, DurableJobState.Running.ToString(), StringComparison.Ordinal) &&
               receipt is null && evidence is null && evt is null;
    }

    private static async Task<(bool JobState, bool Receipt, bool Evidence, bool Event)> VerifyCommittedStateAsync(string databasePath, TerminalCommitRequest request, CancellationToken cancellationToken)
    {
        var state = await ReadScalarAsync(databasePath, "SELECT state FROM research_jobs WHERE job_id=$job;", request.Job.JobId, cancellationToken);
        var receipt = await ReadScalarAsync(databasePath, "SELECT receipt_fingerprint FROM resource_execution_receipts WHERE receipt_fingerprint=$id;", request.Receipt.ReceiptFingerprint, cancellationToken);
        var evidence = await ReadScalarAsync(databasePath, "SELECT evidence_id FROM evidence WHERE evidence_id=$id;", request.EvidenceId, cancellationToken);
        var evt = await ReadScalarAsync(databasePath, "SELECT event_id FROM research_events WHERE event_id=$id;", request.EventId, cancellationToken);
        return (
            string.Equals(state, DurableJobState.Completed.ToString(), StringComparison.Ordinal),
            string.Equals(receipt, request.Receipt.ReceiptFingerprint, StringComparison.Ordinal),
            string.Equals(evidence, request.EvidenceId, StringComparison.Ordinal),
            string.Equals(evt, request.EventId, StringComparison.Ordinal));
    }

    private static async Task<string?> ReadScalarAsync(string databasePath, string sql, string id, CancellationToken cancellationToken)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly, Cache = SqliteCacheMode.Shared };
        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$job", id);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value == DBNull.Value ? null : value.ToString();
    }

    private static TransactionalTerminalFaultCheck Fail(TransactionalTerminalFaultPoint point, string detail, bool faultContained = false, bool rolledBack = false) =>
        new(point, faultContained, rolledBack, false, false, false, false, false, detail);
}
