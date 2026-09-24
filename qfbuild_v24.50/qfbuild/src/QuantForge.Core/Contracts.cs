namespace QuantForge.Core;

public enum Authority { Human, Ui, DeterministicPolicy, BoundedBot, EvidenceReceipt, AiAnalysis, IndependentAudit }
public enum JobState { Pending, Leased, Running, Completed, Failed, Cancelled, Expired }
public enum EvidenceKind { Source, SafetyAudit, Backtest, Optimization, Robustness, AiAnalysis, IndependentAudit, Report }

public sealed record ResearchJobId(Guid Value);
public sealed record DeviceNodeId(string Value);
public sealed record ExecutionLease(ResearchJobId JobId, DeviceNodeId Owner, DateTimeOffset AcquiredAt, DateTimeOffset ExpiresAt, long Version);
public sealed record EvidenceRecord(string EvidenceId, EvidenceKind Kind, string ContentHash, DateTimeOffset CreatedAt, string Provenance, string EngineVersion);
public sealed record ResearchEvent(string EventId, ResearchJobId JobId, DateTimeOffset Timestamp, string Type, string PayloadHash);

public static class QuantForgeAuthority
{
    public static readonly IReadOnlySet<string> ProhibitedAuthorities = new HashSet<string>(StringComparer.Ordinal)
    {
        "LIVE_ORDER", "LIVE_TRADING", "ARBITRARY_CODE_EXECUTION", "CANONICAL_DATA_MUTATION", "UNDECLARED_APP_MUTATION", "MANIFEST_MUTATION"
    };
}
