namespace QuantForge.Governance;

public enum LiveTradingSecurityEvent
{
    PathwayEnabled,
    PathwayDisabled,
    SettingsChanged,
    StepUpRequested,
    StepUpFailed,
    StepUpSucceeded,
    ArmRequested,
    Armed,
    DisarmRequested,
    Disarmed,
    DependencyBlocked,
    CommunicationBlocked,
    SessionExpired,
    EmergencyDisarm,
    ExecutionIntentPrepared,
    ExecutionSubmissionAccepted,
    ExecutionSubmissionRejected,
    ExecutionOutcomeUnknown,
    ExecutionReconciled
}

public sealed record LiveTradingAuditRecord(
    string EventId,
    LiveTradingSecurityEvent Event,
    DateTimeOffset OccurredAt,
    string UserId,
    string Reason,
    string SettingsFingerprint,
    string? SessionId,
    string? CorrelationId);

public interface ILiveTradingAuditSink
{
    void Append(LiveTradingAuditRecord record);
}

public sealed class InMemoryLiveTradingAuditSink : ILiveTradingAuditSink
{
    private readonly List<LiveTradingAuditRecord> _records = [];
    public IReadOnlyList<LiveTradingAuditRecord> Records => _records.AsReadOnly();
    public void Append(LiveTradingAuditRecord record) => _records.Add(record);
}
