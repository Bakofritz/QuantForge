using System.Security.Cryptography;
using System.Text;
using QuantForge.Core;

namespace QuantForge.Governance;

public sealed class StrategyProvenanceService
{
    public StrategyProvenanceRecord Create(StrategyAuditReport audit, StrategyComponentSelection selection, StrategyReviewDecisionRecord decision, StrategyGovernanceCommitReceipt receipt, CanonicalStrategyModel model, CanonicalStrategySemantics? semantics = null, DateTimeOffset? recordedAt = null)
    {
        if (audit.SourceId != selection.SourceId || selection.SourceId != decision.SourceId || decision.SourceId != receipt.SourceId || receipt.SourceId != model.SourceId) throw new InvalidOperationException("Strategy provenance lineage is not contiguous.");
        if (audit.AuditFingerprint != decision.AuditFingerprint || selection.SelectionFingerprint != decision.SelectionFingerprint || receipt.ModelFingerprint != model.ModelFingerprint) throw new InvalidOperationException("Strategy provenance fingerprint chain is broken.");
        if (semantics is not null && semantics.ModelFingerprint != model.ModelFingerprint) throw new InvalidOperationException("Canonical semantics are bound to a different model.");
        var at = recordedAt ?? DateTimeOffset.UtcNow;
        var fp = Hash(string.Join("|", audit.ContentHash, audit.AuditFingerprint, selection.SelectionFingerprint, decision.DecisionFingerprint, receipt.ReceiptFingerprint, model.ModelFingerprint, semantics?.SemanticsFingerprint ?? "", at.ToUnixTimeMilliseconds()));
        return new StrategyProvenanceRecord(audit.SourceId, audit.ContentHash, audit.Language, audit.AuditFingerprint, selection.SelectionFingerprint, decision.DecisionFingerprint, receipt.ReceiptFingerprint, model.ModelFingerprint, semantics?.SemanticsFingerprint, at, fp);
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
