using QuantForge.Core;

namespace QuantForge.Governance;

/// <summary>Research strategy registry. Registration requires a contiguous governed provenance chain.</summary>
public sealed class StrategyRegistryService
{
    private readonly StrategyCapabilityManifestService _capabilities;
    public StrategyRegistryService(StrategyCapabilityManifestService? capabilities = null) => _capabilities = capabilities ?? new StrategyCapabilityManifestService();

    public StrategyRegistryEntry RegisterResearchStrategy(CanonicalStrategyModel model, StrategyProvenanceRecord provenance, StrategyCapabilityManifest manifest, DateTimeOffset? registeredAt = null)
    {
        ArgumentNullException.ThrowIfNull(model); ArgumentNullException.ThrowIfNull(provenance); ArgumentNullException.ThrowIfNull(manifest);
        _capabilities.AssertResearchOnly(manifest);
        if (provenance.ModelFingerprint != model.ModelFingerprint || manifest.ModelFingerprint != model.ModelFingerprint) throw new InvalidOperationException("Registry inputs do not share the same canonical model fingerprint.");
        if (!model.ResearchEligible || model.LiveDeploymentEligible) throw new InvalidOperationException("Only research-eligible, non-live canonical models may enter the research registry.");
        return new StrategyRegistryEntry(model.StrategyId, model.SourceId, model.ModelFingerprint, StrategyLifecycleState.ResearchRegistered, provenance.ProvenanceFingerprint, manifest.ManifestFingerprint, true, false, registeredAt ?? DateTimeOffset.UtcNow);
    }
}
