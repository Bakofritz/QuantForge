using System.Security.Cryptography;
using System.Text;
using QuantForge.Core;

namespace QuantForge.Governance;

/// <summary>Creates explicit least-privilege capability manifests. Research approval never grants live order authority.</summary>
public sealed class StrategyCapabilityManifestService
{
    public StrategyCapabilityManifest CreateResearchManifest(CanonicalStrategyModel model, StrategyReviewDecisionRecord decision, DateTimeOffset? issuedAt = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.Decision != StrategyReviewDecision.ApprovedForResearch || decision.Authority != StrategyCapabilityAuthority.ReadOnlyResearch)
            throw new InvalidOperationException("A research capability manifest requires explicit read-only research approval.");
        if (!string.Equals(decision.SourceId, model.SourceId, StringComparison.Ordinal)) throw new InvalidOperationException("Decision/source mismatch.");
        var granted = new HashSet<StrategyCapability> { StrategyCapability.StaticAudit, StrategyCapability.ReadOnlyResearch, StrategyCapability.ParameterOptimization, StrategyCapability.HistoricalMarketDataRead, StrategyCapability.MultiScriptBatchRead };
        var denied = new HashSet<StrategyCapability> { StrategyCapability.LiveOrderSubmission };
        var at = issuedAt ?? DateTimeOffset.UtcNow;
        var fp = Hash(string.Join("|", model.StrategyId, model.ModelFingerprint, decision.DecisionFingerprint, string.Join(",", granted.Order()), string.Join(",", denied.Order()), at.ToUnixTimeMilliseconds()));
        return new StrategyCapabilityManifest(model.StrategyId, model.SourceId, model.ModelFingerprint, granted, denied, fp, at, true);
    }

    public void AssertResearchOnly(StrategyCapabilityManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!manifest.IsResearchOnly || !manifest.GrantedCapabilities.Contains(StrategyCapability.ReadOnlyResearch) || manifest.GrantedCapabilities.Contains(StrategyCapability.LiveOrderSubmission) || !manifest.DeniedCapabilities.Contains(StrategyCapability.LiveOrderSubmission))
            throw new InvalidOperationException("Capability manifest violates the research-only authority boundary.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
