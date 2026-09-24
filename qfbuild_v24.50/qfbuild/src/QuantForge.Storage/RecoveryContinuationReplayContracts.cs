using QuantForge.Core;

namespace QuantForge.Storage;

public enum RecoveryContinuationOperationState
{
    Prepared,
    ResultRecorded
}

public sealed record RecoveryContinuationOperation(
    string OperationFingerprint,
    string JobId,
    long Sequence,
    int Cursor,
    string StateHash,
    RecoveryContinuationOperationState State,
    int? NextCursor,
    string? ResultStateHash,
    bool? Completed,
    string? ResultFingerprint,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResultRecordedAt);

public interface IRecoveryContinuationReplayStore
{
    Task<RecoveryContinuationOperation?> LoadAsync(string operationFingerprint, CancellationToken cancellationToken = default);
    Task PrepareAsync(RecoveryContinuationOperation operation, CancellationToken cancellationToken = default);
    Task RecordResultAsync(string operationFingerprint, int nextCursor, string stateHash, bool completed, string? resultFingerprint, CancellationToken cancellationToken = default);
}

/// <summary>
/// Native-storage seam that atomically records a continuation result and advances its
/// immutable checkpoint. Implementations must commit both durable mutations together.
/// </summary>
public interface IRecoveryContinuationAtomicCommitStore
{
    Task CommitResultAndCheckpointAsync(
        string operationFingerprint,
        int nextCursor,
        string stateHash,
        bool completed,
        string? resultFingerprint,
        ResearchCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}
