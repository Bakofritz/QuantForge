using System.Security.Cryptography;
using System.Text;
using QuantForge.Core;

namespace QuantForge.Governance;

/// <summary>Final pre-canonical-commit gate. It creates a research receipt, never live execution authority.</summary>
public sealed class StrategyGovernanceCommitGate
{
    public StrategyGovernanceCommitReceipt CommitResearchModel(
        StrategyAuditReport audit,
        StrategyComponentSelection selection,
        CanonicalStrategyModel model,
        StrategyReviewDecisionRecord decision)
    {
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.Decision != StrategyReviewDecision.ApprovedForResearch || decision.Authority != StrategyCapabilityAuthority.ReadOnlyResearch)
            throw new InvalidOperationException("Canonical research commit requires explicit read-only research approval.");
        if (!string.Equals(decision.SourceId, audit.SourceId, StringComparison.Ordinal) || !string.Equals(decision.SelectionFingerprint, selection.SelectionFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Governance decision does not match the current source selection.");
        if (!string.Equals(model.SourceId, audit.SourceId, StringComparison.Ordinal) || !string.Equals(model.SourceFingerprint, audit.ContentHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Canonical model does not match the audited source.");
        if (model.LiveDeploymentEligible)
            throw new InvalidOperationException("Research commit cannot grant live deployment eligibility.");
        var receipt = Hash(string.Join("|", decision.DecisionFingerprint, model.ModelFingerprint, StrategyCapabilityAuthority.ReadOnlyResearch));
        return new StrategyGovernanceCommitReceipt(audit.SourceId, audit.AuditFingerprint, selection.SelectionFingerprint, model.ModelFingerprint, StrategyCapabilityAuthority.ReadOnlyResearch, receipt);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
