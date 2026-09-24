using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record BatchAccountingReconciliationResult(
    string BatchId,
    string BatchFingerprint,
    int TotalCases,
    int CompletedCases,
    int FailedCases,
    int CanceledCases,
    long TotalElapsedMilliseconds,
    int TotalHeartbeatRenewals,
    string ResourceStatus,
    string RecordFingerprint,
    IReadOnlyList<string> CountedReceiptFingerprints);

/// <summary>
/// Rebuilds batch resource accounting from immutable execution receipts. A context contributes
/// exactly one terminal receipt to accounting, so an interrupted attempt followed by a successful
/// continuation is not double-counted. Receipt history itself remains preserved.
/// </summary>
public sealed class BatchAccountingReconciler
{
    private readonly IResourceReceiptStore _receipts;
    private readonly ILocalEvidenceStore? _evidence;

    public BatchAccountingReconciler(IResourceReceiptStore receipts, ILocalEvidenceStore? evidence = null)
    {
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        _evidence = evidence;
    }

    public async Task<BatchAccountingReconciliationResult> ReconcileAsync(
        string batchId,
        string batchFingerprint,
        int expectedCaseCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);
        ArgumentException.ThrowIfNullOrWhiteSpace(batchFingerprint);
        if (expectedCaseCount < 1) throw new ArgumentOutOfRangeException(nameof(expectedCaseCount));

        var history = await _receipts.LoadExecutionReceiptsForBatchAsync(batchId, cancellationToken);
        var counted = history
            .GroupBy(x => x.ContextId, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(x => x.CompletedAt).ThenByDescending(x => x.ReceiptFingerprint, StringComparer.Ordinal).First())
            .OrderBy(x => x.ContextId, StringComparer.Ordinal)
            .ToArray();

        var completed = counted.Count(x => string.Equals(x.State, nameof(MultiCaseItemState.Completed), StringComparison.Ordinal));
        var failed = counted.Count(x => string.Equals(x.State, nameof(MultiCaseItemState.Failed), StringComparison.Ordinal));
        var canceled = counted.Count(x => string.Equals(x.State, nameof(MultiCaseItemState.Canceled), StringComparison.Ordinal));
        var status = counted.Length < expectedCaseCount
            ? "ACTIVE"
            : canceled > 0
                ? "CANCELED"
                : failed > 0
                    ? "COMPLETED_WITH_FAILURES"
                    : completed == expectedCaseCount ? "COMPLETED" : "ACTIVE";

        var totalElapsed = counted.Sum(x => x.ElapsedMilliseconds);
        var totalHeartbeats = counted.Sum(x => x.HeartbeatRenewals);
        var receiptIds = counted.Select(x => x.ReceiptFingerprint).ToArray();
        var recordFingerprint = ResearchFingerprint.Sha256(string.Join("|", new[]
        {
            batchId, batchFingerprint, expectedCaseCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            completed.ToString(System.Globalization.CultureInfo.InvariantCulture), failed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            canceled.ToString(System.Globalization.CultureInfo.InvariantCulture), totalElapsed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            totalHeartbeats.ToString(System.Globalization.CultureInfo.InvariantCulture), status, string.Join(",", receiptIds)
        }));

        var workers = counted.Select(x => x.WorkerId).Distinct(StringComparer.Ordinal).ToArray();
        var workerFingerprints = counted.Select(x => x.WorkerFingerprint).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var workerId = workers.Length == 1 ? workers[0] : (workers.Length == 0 ? "NO_WORKER" : "MULTI_WORKER");
        var workerFingerprint = ResearchFingerprint.Sha256(string.Join("|", workerFingerprints));

        await _receipts.AppendBatchResourceRecordAsync(new BatchResourceRecord(
            batchId, workerId, workerFingerprint, batchFingerprint, expectedCaseCount,
            completed, failed, canceled, totalElapsed, totalHeartbeats, status, recordFingerprint, DateTimeOffset.UtcNow), cancellationToken);

        if (_evidence is not null)
        {
            await _evidence.AppendEvidenceAsync($"batch-accounting:{recordFingerprint}", recordFingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:batch-accounting:{recordFingerprint}", batchId, "BATCH_ACCOUNTING_RECONCILED", recordFingerprint, cancellationToken);
        }

        return new(batchId, batchFingerprint, expectedCaseCount, completed, failed, canceled,
            totalElapsed, totalHeartbeats, status, recordFingerprint, receiptIds);
    }
}
