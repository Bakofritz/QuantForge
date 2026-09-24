namespace QuantForge.Core;

public enum StrategyReviewDecision { ReviewRequired, ApprovedForResearch, Rejected }
public enum StrategyCapabilityAuthority { None, ReadOnlyResearch }

public sealed record StrategyReviewDecisionRecord(
    string SourceId,
    string AuditFingerprint,
    string SelectionFingerprint,
    StrategyReviewDecision Decision,
    StrategyCapabilityAuthority Authority,
    string ReviewerReference,
    DateTimeOffset RecordedAt,
    string DecisionFingerprint);

public sealed record StrategyGovernanceCommitReceipt(
    string SourceId,
    string AuditFingerprint,
    string SelectionFingerprint,
    string ModelFingerprint,
    StrategyCapabilityAuthority Authority,
    string ReceiptFingerprint);
