namespace QuantForge.Scout;

public sealed record ScoutCandidate(string CandidateId, string SourceUrl, string Provider, string ContentHash, bool ExecutionAllowed, bool CanonicalMutationAllowed);
public sealed record SafetyAudit(string CandidateId, string Status, IReadOnlyList<string> Signals, string Limitations);

public interface IScoutGateway
{
    Task<ScoutCandidate> QuarantineAsync(string sourceUrl, CancellationToken cancellationToken = default);
    Task<SafetyAudit> AuditAsync(ScoutCandidate candidate, CancellationToken cancellationToken = default);
}
