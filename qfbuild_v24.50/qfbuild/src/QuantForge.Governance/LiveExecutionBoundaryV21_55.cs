using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuantForge.Governance;

public enum LiveExecutionIntentState
{
    Prepared,
    HandoffAuthorized,
    Submitted,
    Acknowledged,
    Rejected,
    Unknown,
    Reconciled,
    Canceled
}

public sealed record LiveExecutionIntent(
    string IntentId,
    string IdempotencyKey,
    string PreviewId,
    string PreviewFingerprint,
    string SecurityBindingFingerprint,
    string OrderPayloadFingerprint,
    DateTimeOffset CreatedAt,
    LiveExecutionIntentState State,
    string? ExternalOrderId,
    string? BrokerRequestFingerprint);

public sealed record LiveBrokerSubmissionRequest(
    string IdempotencyKey,
    string PreviewId,
    string OrderPayloadFingerprint,
    string SecurityBindingFingerprint,
    DateTimeOffset RequestedAt);

public sealed record LiveBrokerSubmissionResult(
    bool Accepted,
    bool Definitive,
    string Code,
    string Message,
    string? ExternalOrderId,
    string? BrokerRequestFingerprint,
    DateTimeOffset ObservedAt);

/// <summary>Broker execution adapter contract. Implementations must be isolated from authorization and admission policy.</summary>
public interface ILiveBrokerExecutionAdapter
{
    LiveBrokerSubmissionResult Submit(LiveBrokerSubmissionRequest request);
    LiveBrokerSubmissionResult QueryByIdempotencyKey(string idempotencyKey, DateTimeOffset now);
}

public interface ILiveExecutionIntentStore
{
    LiveExecutionIntent? GetByIdempotencyKey(string idempotencyKey);
    LiveExecutionIntent Save(LiveExecutionIntent intent);
    LiveExecutionIntent Update(LiveExecutionIntent intent);
}

public sealed class InMemoryLiveExecutionIntentStore : ILiveExecutionIntentStore
{
    private readonly Dictionary<string, LiveExecutionIntent> _byKey = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public LiveExecutionIntent? GetByIdempotencyKey(string idempotencyKey)
    {
        lock (_gate) return _byKey.TryGetValue(idempotencyKey, out var value) ? value : null;
    }

    public LiveExecutionIntent Save(LiveExecutionIntent intent)
    {
        lock (_gate)
        {
            if (_byKey.ContainsKey(intent.IdempotencyKey)) throw new InvalidOperationException("IDEMPOTENCY_KEY_ALREADY_EXISTS");
            _byKey[intent.IdempotencyKey] = intent;
            return intent;
        }
    }

    public LiveExecutionIntent Update(LiveExecutionIntent intent)
    {
        lock (_gate)
        {
            if (!_byKey.ContainsKey(intent.IdempotencyKey)) throw new KeyNotFoundException("EXECUTION_INTENT_NOT_FOUND");
            _byKey[intent.IdempotencyKey] = intent;
            return intent;
        }
    }
}

public sealed record LiveExecutionHandoffResult(
    bool Allowed,
    string Code,
    string Reason,
    LiveExecutionIntent? Intent,
    LiveBrokerSubmissionResult? BrokerResult);

/// <summary>
/// Final execution coordinator. It revalidates admission, consumes confirmation exactly once,
/// records an idempotent intent before submission, and fails closed on ambiguous broker outcomes.
/// It never retries an ambiguous submission automatically.
/// </summary>
public sealed class LiveExecutionCoordinator
{
    private readonly ILiveBrokerExecutionAdapter _broker;
    private readonly ILiveExecutionIntentStore _store;
    private readonly LiveExecutionAuthorityBoundary _authority;
    private readonly ITamperEvidentAuditSink? _audit;

    public LiveExecutionCoordinator(ILiveBrokerExecutionAdapter broker, ILiveExecutionIntentStore store, LiveExecutionAuthorityBoundary? authority = null, ITamperEvidentAuditSink? audit = null)
    {
        _broker = broker;
        _store = store;
        _authority = authority ?? new LiveExecutionAuthorityBoundary();
        _audit = audit;
    }

    public LiveExecutionHandoffResult Execute(
        LiveOrderConfirmationResult confirmation,
        LiveOrderPreview preview,
        LiveOrderAdmissionRequest currentAdmission,
        string securityBindingFingerprint,
        DateTimeOffset now)
    {
        if (_audit is not null && !_audit.ChainValid())
            return new(false, "LIVE_AUDIT_CHAIN_INVALID", "Tamper-evident live audit chain is invalid; execution is blocked.", null, null);

        var authorization = _authority.AuthorizeBrokerHandoff(confirmation, preview, currentAdmission);
        if (!authorization.Allowed)
            return new(false, authorization.Code, authorization.Reason, null, null);

        if (confirmation.Token is null)
            return new(false, "CONFIRMATION_REQUIRED", "A confirmed token is required.", null, null);

        var idempotencyKey = BuildIdempotencyKey(preview.PreviewId, preview.AdmissionFingerprint, securityBindingFingerprint);
        var existing = _store.GetByIdempotencyKey(idempotencyKey);
        if (existing is not null)
        {
            return existing.State switch
            {
                LiveExecutionIntentState.Submitted or LiveExecutionIntentState.Acknowledged or LiveExecutionIntentState.Reconciled
                    => new(false, "DUPLICATE_EXECUTION_BLOCKED", "An execution intent already exists for this exact order/security binding.", existing, null),
                LiveExecutionIntentState.Unknown
                    => new(false, "AMBIGUOUS_EXECUTION_REQUIRES_RECONCILIATION", "A prior execution outcome is ambiguous; broker reconciliation is required before any further submission.", existing, null),
                _ => new(false, "EXECUTION_INTENT_ALREADY_EXISTS", "An execution intent already exists and must be reconciled before proceeding.", existing, null)
            };
        }

        var intent = new LiveExecutionIntent(
            $"exec-{Guid.NewGuid():N}",
            idempotencyKey,
            preview.PreviewId,
            preview.AdmissionFingerprint,
            securityBindingFingerprint,
            preview.OrderPayloadFingerprint,
            now,
            LiveExecutionIntentState.HandoffAuthorized,
            null,
            null);
        _store.Save(intent);
        AppendAudit(LiveTradingSecurityEvent.ExecutionIntentPrepared, currentAdmission.Session?.SessionId, "Execution intent durably prepared before broker submission.", now);

        LiveBrokerSubmissionResult result;
        try
        {
            result = _broker.Submit(new(idempotencyKey, preview.PreviewId, preview.OrderPayloadFingerprint, securityBindingFingerprint, now));
        }
        catch (Exception ex)
        {
            var unknown = intent with { State = LiveExecutionIntentState.Unknown };
            _store.Update(unknown);
            AppendAudit(LiveTradingSecurityEvent.ExecutionOutcomeUnknown, currentAdmission.Session?.SessionId, "Broker submission threw before a definitive outcome was observed; reconciliation required.", now);
            return new(false, "BROKER_OUTCOME_UNKNOWN", $"Broker submission outcome is unknown and requires reconciliation: {ex.Message}", unknown, null);
        }

        var nextState = result.Definitive
            ? (result.Accepted ? LiveExecutionIntentState.Acknowledged : LiveExecutionIntentState.Rejected)
            : LiveExecutionIntentState.Unknown;
        var updated = intent with
        {
            State = nextState,
            ExternalOrderId = result.ExternalOrderId,
            BrokerRequestFingerprint = result.BrokerRequestFingerprint
        };
        _store.Update(updated);
        if (result.Definitive && result.Accepted)
            AppendAudit(LiveTradingSecurityEvent.ExecutionSubmissionAccepted, currentAdmission.Session?.SessionId, result.Message, now);
        else if (result.Definitive)
            AppendAudit(LiveTradingSecurityEvent.ExecutionSubmissionRejected, currentAdmission.Session?.SessionId, result.Message, now);
        else
            AppendAudit(LiveTradingSecurityEvent.ExecutionOutcomeUnknown, currentAdmission.Session?.SessionId, "Broker returned a non-definitive outcome; reconciliation required.", now);

        return result.Definitive && result.Accepted
            ? new(true, "BROKER_SUBMISSION_ACCEPTED", result.Message, updated, result)
            : result.Definitive
                ? new(false, "BROKER_SUBMISSION_REJECTED", result.Message, updated, result)
                : new(false, "BROKER_OUTCOME_UNKNOWN", "Broker did not provide a definitive outcome. Reconciliation is required and automatic retry is forbidden.", updated, result);
    }

    private void AppendAudit(LiveTradingSecurityEvent eventKind, string? sessionId, string reason, DateTimeOffset now)
    {
        if (_audit is null) return;
        if (!_audit.ChainValid()) throw new InvalidOperationException("LIVE_AUDIT_CHAIN_INVALID");
        _audit.Append(new LiveTradingAuditRecord(
            $"audit-{Guid.NewGuid():N}", eventKind, now, "SYSTEM", reason, "execution-boundary", sessionId, null));
    }

    private static string BuildIdempotencyKey(string previewId, string previewFingerprint, string securityBindingFingerprint)
    {
        var canonical = $"{previewId}|{previewFingerprint}|{securityBindingFingerprint}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}

public sealed record LiveExecutionReconciliationResult(bool Resolved, string Code, string Reason, LiveExecutionIntent? Intent);

/// <summary>Reconciles only by querying the broker; it never creates a second submission for an ambiguous intent.</summary>
public sealed class LiveExecutionReconciler
{
    private readonly ILiveBrokerExecutionAdapter _broker;
    private readonly ILiveExecutionIntentStore _store;
    private readonly ITamperEvidentAuditSink? _audit;

    public LiveExecutionReconciler(ILiveBrokerExecutionAdapter broker, ILiveExecutionIntentStore store, ITamperEvidentAuditSink? audit = null)
    {
        _broker = broker;
        _store = store;
        _audit = audit;
    }

    public LiveExecutionReconciliationResult Reconcile(string idempotencyKey, DateTimeOffset now)
    {
        var intent = _store.GetByIdempotencyKey(idempotencyKey);
        if (intent is null) return new(false, "EXECUTION_INTENT_NOT_FOUND", "No execution intent exists for the supplied idempotency key.", null);
        if (intent.State != LiveExecutionIntentState.Unknown)
            return new(true, "NO_AMBIGUITY", "The execution intent is already in a definitive or reconciled state.", intent);

        LiveBrokerSubmissionResult result;
        try { result = _broker.QueryByIdempotencyKey(idempotencyKey, now); }
        catch (Exception ex) { return new(false, "RECONCILIATION_UNAVAILABLE", $"Broker reconciliation failed: {ex.Message}", intent); }

        if (!result.Definitive)
            return new(false, "RECONCILIATION_STILL_AMBIGUOUS", "The broker still cannot provide a definitive outcome. No new submission is permitted.", intent);

        var next = intent with
        {
            State = result.Accepted ? LiveExecutionIntentState.Reconciled : LiveExecutionIntentState.Rejected,
            ExternalOrderId = result.ExternalOrderId ?? intent.ExternalOrderId,
            BrokerRequestFingerprint = result.BrokerRequestFingerprint ?? intent.BrokerRequestFingerprint
        };
        _store.Update(next);
        if (_audit is not null)
        {
            if (!_audit.ChainValid()) return new(false, "LIVE_AUDIT_CHAIN_INVALID", "Audit chain became invalid during reconciliation.", intent);
            _audit.Append(new LiveTradingAuditRecord($"audit-{Guid.NewGuid():N}", LiveTradingSecurityEvent.ExecutionReconciled, now, "SYSTEM", result.Message, "execution-reconciliation", null, null));
        }
        return new(true, result.Accepted ? "EXECUTION_RECONCILED_ACCEPTED" : "EXECUTION_RECONCILED_REJECTED", result.Message, next);
    }
}
