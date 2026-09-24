using System.Security.Cryptography;
using System.Text;
using QuantForge.Core;

namespace QuantForge.Governance;

public sealed record QuarantinedStrategySource(StrategySourceArtifact Artifact, string SourceText, StrategyAuditReport Audit);

/// <summary>Governed acquisition facade. Acquisition and audit do not grant execution or application authority.</summary>
public sealed class StrategyAcquisitionService
{
    private readonly StrategySourceScrubber _scrubber;

    public StrategyAcquisitionService(StrategySourceScrubber? scrubber = null) => _scrubber = scrubber ?? new StrategySourceScrubber();

    public QuarantinedStrategySource AcquireAndAudit(string sourceId, string source, StrategySourceLanguage language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(source);
        var audit = _scrubber.Audit(sourceId, source, language);
        var artifact = new StrategySourceArtifact(sourceId, language, audit.ContentHash, source.Length, DateTimeOffset.UtcNow, StrategySourceStatus.Quarantined);
        return new QuarantinedStrategySource(artifact, source, audit);
    }

    public StrategyComponentSelection CommitSelection(QuarantinedStrategySource quarantined, IEnumerable<string> selectedFeatureIds)
    {
        if (quarantined.Audit.Features.Any(f => f.IsSafetySensitive && selectedFeatureIds.Contains(f.FeatureId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Safety-sensitive source capabilities cannot cross the canonical boundary through selection alone; governance review is required.");
        return _scrubber.Select(quarantined.Audit, selectedFeatureIds, commitRequested: true);
    }

    public CanonicalStrategyModel BuildResearchModel(QuarantinedStrategySource quarantined, StrategyComponentSelection selection)
        => _scrubber.BuildCanonicalModel(quarantined.Audit, selection);
}
