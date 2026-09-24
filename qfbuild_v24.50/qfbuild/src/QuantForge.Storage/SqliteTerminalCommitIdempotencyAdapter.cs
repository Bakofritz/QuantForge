using Microsoft.Data.Sqlite;
using QuantForge.Core;

namespace QuantForge.Storage;

public sealed record TerminalCommitIdempotencyCheck(string Scenario, bool Passed, string Detail);

public sealed record TerminalCommitIdempotencyValidationResult(
    string HarnessVersion,
    bool Passed,
    IReadOnlyList<TerminalCommitIdempotencyCheck> Checks,
    string ResultFingerprint,
    bool NativeStorageExecuted);

/// <summary>
/// Native SQLite validation adapter for terminal duplicate-result protection.
/// Execution is explicit and never occurs as part of ordinary research.
/// </summary>
public sealed class SqliteTerminalCommitIdempotencyAdapter
{
    public async Task<TerminalCommitIdempotencyValidationResult> RunAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var checks = new List<TerminalCommitIdempotencyCheck>();
        var jobId = "idempotency-test-job";
        var now = DateTimeOffset.UtcNow;
        var job = new ResearchJob(jobId, DurableJobState.Running, "dataset", "config", "engine", now, now);
        var receipt = CreateReceipt(jobId, "result-a", now);
        var request = CreateRequest(job, receipt);

        await using (var seed = new SqliteLocalStore(databasePath))
        {
            await seed.InitializeAsync(cancellationToken);
            await seed.SaveJobAsync(job, cancellationToken);
            if (!await seed.TryAcquireAsync(jobId, "worker-a", TimeSpan.FromMinutes(5), cancellationToken))
                return Fail("Unable to establish seed lease.");
        }

        await using (var first = new SqliteLocalStore(databasePath))
        {
            await first.CommitTerminalOutcomeAsync(request, cancellationToken);
        }

        await using (var duplicate = new SqliteLocalStore(databasePath))
        {
            await duplicate.CommitTerminalOutcomeAsync(request, cancellationToken);
        }

        var count = await CountReceiptsAsync(databasePath, jobId, cancellationToken);
        checks.Add(new TerminalCommitIdempotencyCheck(
            "IDENTICAL_RESULT_RETRY",
            count == 1,
            count == 1 ? "Identical terminal retry was accepted without creating a duplicate receipt." : $"Expected one receipt; found {count}."));

        var conflictRequest = CreateRequest(job, CreateReceipt(jobId, "result-b", now.AddMilliseconds(1)));
        var conflictRejected = false;
        try
        {
            await using var conflict = new SqliteLocalStore(databasePath);
            await conflict.CommitTerminalOutcomeAsync(conflictRequest, cancellationToken);
        }
        catch (TerminalCommitConflictException ex) when (ex.Code == "TERMINAL_COMMIT_RESULT_CONFLICT")
        {
            conflictRejected = true;
        }
        checks.Add(new TerminalCommitIdempotencyCheck(
            "DIFFERENT_RESULT_REJECTED",
            conflictRejected,
            conflictRejected ? "Different terminal result for the same job was rejected." : "Different terminal result was not rejected."));

        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks.Select(c => $"{c.Scenario}:{c.Passed}:{c.Detail}")));
        return new TerminalCommitIdempotencyValidationResult("QF-SQLITE-TERMINAL-IDEMPOTENCY-1", checks.All(c => c.Passed), checks.AsReadOnly(), fingerprint, true);
    }

    private static TerminalCommitReceipt CreateReceipt(string jobId, string result, DateTimeOffset now) =>
        new(ResearchFingerprint.Sha256($"receipt|{jobId}|{result}"), "batch", "context", jobId, "worker-a", "worker-fingerprint", DurableJobState.Completed.ToString(), result, now, now, 1, 0, "TEST", null);

    private static TerminalCommitRequest CreateRequest(ResearchJob job, TerminalCommitReceipt receipt) =>
        new(job, receipt, $"idempotency-evidence:{receipt.ReceiptFingerprint}", ResearchFingerprint.Sha256($"evidence|{receipt.ResultFingerprint}"), $"idempotency-event:{receipt.ReceiptFingerprint}", "TERMINAL_IDEMPOTENCY_TEST", ResearchFingerprint.Sha256($"event|{receipt.ResultFingerprint}"));

    private static async Task<int> CountReceiptsAsync(string databasePath, string jobId, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly, Cache = SqliteCacheMode.Shared }.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM resource_execution_receipts WHERE job_id=$job;";
        command.Parameters.AddWithValue("$job", jobId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static TerminalCommitIdempotencyValidationResult Fail(string detail) =>
        new("QF-SQLITE-TERMINAL-IDEMPOTENCY-1", false,
            new[] { new TerminalCommitIdempotencyCheck("SEED", false, detail) },
            ResearchFingerprint.Sha256($"SEED:false:{detail}"), true);
}
