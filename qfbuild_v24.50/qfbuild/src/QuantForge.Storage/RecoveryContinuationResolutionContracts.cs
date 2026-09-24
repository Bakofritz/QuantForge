using QuantForge.Core;

namespace QuantForge.Storage;

public enum RecoveryContinuationResolutionAction
{
    ConfirmExternalCompletion,
    AbortOperation,
    RequestReplayAuthorization
}

public sealed record RecoveryContinuationResolutionRecord(
    string ResolutionFingerprint,
    string OperationFingerprint,
    string DecisionFingerprint,
    string EvidenceFingerprint,
    RecoveryContinuationResolutionAction Action,
    string ResolverFingerprint,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? AuthorizationNonce,
    bool Consumed,
    DateTimeOffset? ConsumedAt);

public interface IRecoveryContinuationResolutionStore
{
    Task<RecoveryContinuationResolutionRecord?> LoadAsync(string operationFingerprint, CancellationToken cancellationToken = default);
    Task RecordAsync(RecoveryContinuationResolutionRecord record, CancellationToken cancellationToken = default);
    Task<bool> ConsumeAsync(string resolutionFingerprint, string authorizationNonce, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolution records are evidence-bound control records. They never authorize automatic callback replay.
/// </summary>
public static class RecoveryContinuationResolutionPolicy
{
    public const string ExpiredCode = "RECOVERY_CONTINUATION_RESOLUTION_EXPIRED";
    public const string StaleDecisionCode = "RECOVERY_CONTINUATION_RESOLUTION_STALE";
    public const string ReplayRequiresExplicitBoundaryCode = "RECOVERY_CONTINUATION_REPLAY_REQUIRES_EXPLICIT_EXTERNAL_RECONCILIATION";

    public static string Fingerprint(string operation, string decision, string evidence, string resolver) =>
        ResearchFingerprint.Sha256($"QF-RECOVERY-RESOLUTION-V1|{operation}|{decision}|{evidence}|{resolver}");
}
