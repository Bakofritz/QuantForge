namespace QuantForge.Core;

public sealed record TerminalCommitReceipt(
    string ReceiptFingerprint, string BatchId, string ContextId, string JobId,
    string WorkerId, string WorkerFingerprint, string State, string ResultFingerprint,
    DateTimeOffset StartedAt, DateTimeOffset CompletedAt, long ElapsedMilliseconds,
    int HeartbeatRenewals, string ResourceStatus, string? ErrorCode);

public sealed record TerminalCommitRequest(
    ResearchJob Job, TerminalCommitReceipt Receipt,
    string EvidenceId, string EvidenceHash,
    string EventId, string EventType, string EventPayloadHash);

public interface ITerminalCommitStore
{
    Task CommitTerminalOutcomeAsync(TerminalCommitRequest request, CancellationToken cancellationToken = default);
}
