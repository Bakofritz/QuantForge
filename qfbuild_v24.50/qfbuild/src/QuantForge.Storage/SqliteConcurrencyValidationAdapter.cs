using QuantForge.Core;

namespace QuantForge.Storage;

/// <summary>
/// Exercises contention against separate SqliteLocalStore instances sharing one database file.
/// This is a validation adapter, not a parallel research executor.
/// </summary>
public sealed class SqliteConcurrencyValidationAdapter : IConcurrencyStorageValidationAdapter
{
    private readonly string _databasePath;

    public SqliteConcurrencyValidationAdapter(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = databasePath;
    }

    public async Task<StorageConcurrencyValidationResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<StorageConcurrencyValidationCheck>();
        await using var first = new SqliteLocalStore(_databasePath);
        await using var second = new SqliteLocalStore(_databasePath);
        await first.InitializeAsync(cancellationToken);
        await second.InitializeAsync(cancellationToken);

        checks.Add(await CheckLeaseRaceAsync(first, second, cancellationToken));
        await first.ReleaseAsync("sqlite-contention-job", "worker-a", CancellationToken.None);
        checks.Add(await CheckReceiptIdempotencyAsync(first, second, cancellationToken));
        checks.Add(await CheckReceiptConflictAsync(first, second, cancellationToken));
        checks.Add(await CheckCheckpointConflictAsync(first, second, cancellationToken));
        checks.Add(await CheckConcurrentReadsAfterWritesAsync(first, second, cancellationToken));

        var fingerprint = ResearchFingerprint.Sha256(string.Join("|", checks.Select(c => $"{c.Name}:{c.Passed}:{c.Detail}")));
        return new StorageConcurrencyValidationResult(
            "QF-SQLITE-CONCURRENCY-ADAPTER-1",
            checks.All(c => c.Passed),
            checks.AsReadOnly(),
            fingerprint,
            true);
    }

    private static async Task<StorageConcurrencyValidationCheck> CheckLeaseRaceAsync(
        IJobLeaseStore first, IJobLeaseStore second, CancellationToken cancellationToken)
    {
        var tasks = new[]
        {
            first.TryAcquireAsync("sqlite-contention-job", "worker-a", TimeSpan.FromSeconds(30), cancellationToken),
            second.TryAcquireAsync("sqlite-contention-job", "worker-b", TimeSpan.FromSeconds(30), cancellationToken)
        };
        var results = await Task.WhenAll(tasks);
        var passed = results.Count(x => x) == 1;
        return new("SQLITE_LEASE_RACE", passed,
            passed ? "Two independent SQLite connections produced exactly one lease owner." : "SQLite lease race did not produce exactly one owner.");
    }

    private static async Task<StorageConcurrencyValidationCheck> CheckReceiptIdempotencyAsync(
        IResourceReceiptStore first, IResourceReceiptStore second, CancellationToken cancellationToken)
    {
        var receipt = CreateReceipt("sqlite-idempotent-receipt", "same-result");
        var tasks = new[]
        {
            first.AppendExecutionReceiptAsync(receipt, cancellationToken),
            second.AppendExecutionReceiptAsync(receipt, cancellationToken)
        };
        try
        {
            await Task.WhenAll(tasks);
            var loaded = await first.LoadExecutionReceiptAsync(receipt.ReceiptFingerprint, cancellationToken);
            var passed = loaded is not null && loaded.ResultFingerprint == receipt.ResultFingerprint;
            return new("SQLITE_RECEIPT_IDEMPOTENCY", passed,
                passed ? "Concurrent identical receipt writes converged to one immutable record." : "Identical receipt writes did not converge.");
        }
        catch (Exception ex)
        {
            return new("SQLITE_RECEIPT_IDEMPOTENCY", false, "Identical receipt writes unexpectedly failed: " + ex.GetType().Name);
        }
    }

    private static async Task<StorageConcurrencyValidationCheck> CheckReceiptConflictAsync(
        IResourceReceiptStore first, IResourceReceiptStore second, CancellationToken cancellationToken)
    {
        var fingerprint = "sqlite-conflict-receipt";
        var original = CreateReceipt(fingerprint, "original-result");
        var conflict = CreateReceipt(fingerprint, "different-result");
        await first.AppendExecutionReceiptAsync(original, cancellationToken);
        try
        {
            await second.AppendExecutionReceiptAsync(conflict, cancellationToken);
            return new("SQLITE_RECEIPT_CONFLICT", false, "Contradictory receipt identity was accepted.");
        }
        catch (InvalidOperationException)
        {
            var loaded = await first.LoadExecutionReceiptAsync(fingerprint, cancellationToken);
            var passed = loaded is not null && loaded.ResultFingerprint == "original-result";
            return new("SQLITE_RECEIPT_CONFLICT", passed,
                passed ? "Contradictory receipt identity was rejected and the original remained intact." : "Receipt conflict was rejected but original state was not preserved.");
        }
    }

    private static async Task<StorageConcurrencyValidationCheck> CheckCheckpointConflictAsync(
        IResearchJobStore first, IResearchJobStore second, CancellationToken cancellationToken)
    {
        var baseline = new ResearchCheckpoint("sqlite-checkpoint-job", 1, 10, "dataset-a", "config-a", "engine-a", "state-a", DateTimeOffset.UtcNow);
        await first.SaveCheckpointAsync(baseline, cancellationToken);
        var conflict = baseline with { StateHash = "state-b" };
        try
        {
            await second.SaveCheckpointAsync(conflict, cancellationToken);
            return new("SQLITE_CHECKPOINT_IMMUTABILITY", false, "A conflicting checkpoint was accepted for an existing sequence.");
        }
        catch (InvalidOperationException)
        {
            var loaded = await first.LoadLatestCheckpointAsync(baseline.JobId, cancellationToken);
            var passed = loaded is not null && loaded.StateHash == baseline.StateHash;
            return new("SQLITE_CHECKPOINT_IMMUTABILITY", passed,
                passed ? "Conflicting checkpoint was rejected and the original checkpoint remained intact." : "Checkpoint conflict was rejected but original state was not preserved.");
        }
    }

    private static async Task<StorageConcurrencyValidationCheck> CheckConcurrentReadsAfterWritesAsync(
        IResourceReceiptStore first, IResourceReceiptStore second, CancellationToken cancellationToken)
    {
        var receipts = Enumerable.Range(0, 8)
            .Select(i => CreateReceipt($"sqlite-read-receipt-{i}", $"result-{i}"))
            .ToArray();
        foreach (var receipt in receipts)
            await first.AppendExecutionReceiptAsync(receipt, cancellationToken);

        var reads = Enumerable.Range(0, 8)
            .Select(_ => first.LoadExecutionReceiptsForBatchAsync("sqlite-contention-batch", cancellationToken));
        await Task.WhenAll(reads);
        var count = (await second.LoadExecutionReceiptsForBatchAsync("sqlite-contention-batch", cancellationToken)).Count;
        var passed = count >= receipts.Length;
        return new("SQLITE_CONCURRENT_READS", passed,
            passed ? "Independent SQLite connections can read persisted evidence after writes." : "Persisted evidence was not visible across SQLite connections.");
    }

    private static ResourceReceiptRecord CreateReceipt(string fingerprint, string resultFingerprint)
    {
        var now = DateTimeOffset.UtcNow;
        return new ResourceReceiptRecord(
            fingerprint,
            "sqlite-contention-batch",
            "sqlite-contention-context",
            fingerprint + "-job",
            "worker-a",
            "worker-a-fingerprint",
            "Completed",
            resultFingerprint,
            now,
            now,
            1,
            0,
            "OK",
            null);
    }
}
