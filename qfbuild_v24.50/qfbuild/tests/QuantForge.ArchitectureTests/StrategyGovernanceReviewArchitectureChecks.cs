using QuantForge.Core;
using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class StrategyGovernanceReviewArchitectureChecks
{
    public static void VerifyAuditDoesNotGrantAuthority()
    {
        var audit = new StrategySourceScrubber().Audit("s", "EMA CrossAbove SMA", StrategySourceLanguage.CSharp);
        if (audit.ExecutionAllowed) throw new InvalidOperationException("Static audit must not grant execution authority.");
    }

    public static void VerifyResearchCommitRequiresExplicitApproval()
    {
        var scrubber = new StrategySourceScrubber();
        var audit = scrubber.Audit("s", "EMA CrossAbove SMA", StrategySourceLanguage.CSharp);
        var selection = scrubber.Select(audit, audit.Features.Select(f => f.FeatureId), true);
        var model = scrubber.BuildCanonicalModel(audit, selection);
        var review = new StrategyGovernanceReviewService();
        var rejected = review.RecordDecision(audit, selection, StrategyReviewDecision.Rejected, StrategyCapabilityAuthority.None, "review-1", DateTimeOffset.UnixEpoch);
        try { new StrategyGovernanceCommitGate().CommitResearchModel(audit, selection, model, rejected); throw new InvalidOperationException("Rejected decision crossed commit gate."); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("explicit read-only research approval", StringComparison.Ordinal)) { }
    }

    public static void VerifyReceiptIsBoundToSelectionAndModel()
    {
        var scrubber = new StrategySourceScrubber();
        var audit = scrubber.Audit("s", "EMA CrossAbove SMA", StrategySourceLanguage.CSharp);
        var selection = scrubber.Select(audit, audit.Features.Select(f => f.FeatureId), true);
        var model = scrubber.BuildCanonicalModel(audit, selection);
        var decision = new StrategyGovernanceReviewService().RecordDecision(audit, selection, StrategyReviewDecision.ApprovedForResearch, StrategyCapabilityAuthority.ReadOnlyResearch, "review-1", DateTimeOffset.UnixEpoch);
        var receipt = new StrategyGovernanceCommitGate().CommitResearchModel(audit, selection, model, decision);
        if (receipt.Authority != StrategyCapabilityAuthority.ReadOnlyResearch || receipt.ModelFingerprint != model.ModelFingerprint) throw new InvalidOperationException("Commit receipt lost governance binding.");
    }
}
