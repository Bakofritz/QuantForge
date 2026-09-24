using System.Security.Cryptography;
using System.Text;
using QuantForge.Core;

namespace QuantForge.Governance;

/// <summary>
/// Explicit human/governance decision boundary. A static audit never grants execution authority.
/// </summary>
public sealed class StrategyGovernanceReviewService
{
    public StrategyReviewDecisionRecord RecordDecision(
        StrategyAuditReport audit,
        StrategyComponentSelection selection,
        StrategyReviewDecision decision,
        StrategyCapabilityAuthority authority,
        string reviewerReference,
        DateTimeOffset? recordedAt = null)
    {
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewerReference);
        if (!string.Equals(audit.SourceId, selection.SourceId, StringComparison.Ordinal))
            throw new InvalidOperationException("Review source does not match the audited source.");
        if (!selection.CommitRequested)
            throw new InvalidOperationException("A governance decision cannot authorize an uncommitted component selection.");
        if (decision != StrategyReviewDecision.ApprovedForResearch && authority != StrategyCapabilityAuthority.None)
            throw new InvalidOperationException("Only an approved research decision may grant research authority.");
        if (authority == StrategyCapabilityAuthority.ReadOnlyResearch && audit.Features.Any(f => f.IsSafetySensitive && selection.SelectedFeatureIds.Contains(f.FeatureId)))
            throw new InvalidOperationException("Safety-sensitive features require an explicit governance review before research authority can be granted.");
        var timestamp = recordedAt ?? DateTimeOffset.UtcNow;
        var fingerprint = Hash(string.Join("|", audit.AuditFingerprint, selection.SelectionFingerprint, decision, authority, reviewerReference, timestamp.ToUnixTimeMilliseconds()));
        return new StrategyReviewDecisionRecord(audit.SourceId, audit.AuditFingerprint, selection.SelectionFingerprint, decision, authority, reviewerReference, timestamp, fingerprint);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
