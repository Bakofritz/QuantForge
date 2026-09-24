using QuantForge.Data;

namespace QuantForge.Storage;

public interface IResourceReceiptStore
{
    Task AppendExecutionReceiptAsync(ResourceReceiptRecord receipt, CancellationToken cancellationToken = default);
    Task<ResourceReceiptRecord?> LoadExecutionReceiptAsync(string receiptFingerprint, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceReceiptRecord>> LoadExecutionReceiptsForJobAsync(string jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceReceiptRecord>> LoadExecutionReceiptsForBatchAsync(string batchId, CancellationToken cancellationToken = default);
    Task AppendBatchResourceRecordAsync(BatchResourceRecord record, CancellationToken cancellationToken = default);
    Task<BatchResourceRecord?> LoadBatchResourceRecordAsync(string batchId, CancellationToken cancellationToken = default);
}

public sealed record ResourceReceiptRecord(
    string ReceiptFingerprint,
    string BatchId,
    string ContextId,
    string JobId,
    string WorkerId,
    string WorkerFingerprint,
    string State,
    string ResultFingerprint,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long ElapsedMilliseconds,
    int HeartbeatRenewals,
    string ResourceStatus,
    string? ErrorCode);

public sealed record BatchResourceRecord(
    string BatchId,
    string WorkerId,
    string WorkerFingerprint,
    string BatchFingerprint,
    int TotalCases,
    int CompletedCases,
    int FailedCases,
    int CanceledCases,
    long TotalElapsedMilliseconds,
    int TotalHeartbeatRenewals,
    string ResourceStatus,
    string RecordFingerprint,
    DateTimeOffset UpdatedAt);

public interface ILocalEvidenceStore
{
    Task AppendEvidenceAsync(string evidenceId, string contentHash, CancellationToken cancellationToken = default);
    Task<bool> ContainsEvidenceAsync(string evidenceId, CancellationToken cancellationToken = default);
    Task AppendEventAsync(string eventId, string jobId, string type, string payloadHash, CancellationToken cancellationToken = default);
}

public interface ILedgerEvidenceStore
{
    Task AppendTradeEvidenceAsync(string jobId, string tradeId, string entryUtc, string exitUtc, int quantity, decimal entryPrice, decimal exitPrice, decimal grossPnl, decimal commission, decimal slippageCost, decimal netPnl, string exitReason, string fingerprint, CancellationToken cancellationToken = default);
    Task AppendLedgerAccountingEventAsync(string jobId, long sequence, string timestampUtc, string sessionKey, string type, int positionBefore, int positionAfter, decimal realizedPnl, decimal dailyRealizedPnl, decimal equity, bool dailyLossLocked, string referenceId, string details, string fingerprint, CancellationToken cancellationToken = default);
}

public interface IMarketDatasetStore
{
    Task SaveDatasetAsync(ImportedMarketDataset dataset, CancellationToken cancellationToken = default);
    Task<ImportedMarketDataset?> LoadDatasetAsync(string datasetId, CancellationToken cancellationToken = default);
}

public interface IJobLeaseStore
{
    Task<bool> TryAcquireAsync(string jobId, string deviceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task<bool> RenewAsync(string jobId, string deviceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task ReleaseAsync(string jobId, string deviceId, CancellationToken cancellationToken = default);
}
