using QuantForge.Core;

namespace QuantForge.Core.Runtime;

/// <summary>Creates independent research contexts. No mutable strategy/position/evidence state is shared between cases.</summary>
public sealed record IsolatedResearchCaseRequest(
    string JobId, string DatasetId, string DatasetFingerprint, string StrategyId,
    string StrategyFingerprint, string Instrument, string Timeframe, string ConfigurationFingerprint);

public sealed class IsolatedResearchCoordinator
{
    public ResearchContextState Create(ResearchContextDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var expected = ResearchContextFingerprint.Create(descriptor.Identity);
        if (!string.Equals(expected, descriptor.Identity.ContextId, StringComparison.Ordinal))
            throw new InvalidOperationException("Research context identity fingerprint is invalid.");
        return new ResearchContextState(descriptor);
    }

    public ResearchContextState Create(IsolatedResearchCaseRequest request, string engineFingerprint)
    {
        ArgumentNullException.ThrowIfNull(request);
        var identityWithoutId = new ResearchContextIdentity(string.Empty, request.JobId, request.DatasetId, request.DatasetFingerprint, request.StrategyId, request.StrategyFingerprint, request.Instrument, request.Timeframe, request.ConfigurationFingerprint);
        var identity = identityWithoutId with { ContextId = ResearchContextFingerprint.Create(identityWithoutId) };
        return Create(new ResearchContextDescriptor(identity, DateTimeOffset.UtcNow, engineFingerprint));
    }

    public IReadOnlyList<ResearchContextState> CreateBatch(IEnumerable<ResearchContextDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var result = descriptors.Select(Create).ToList();
        var duplicate = result.GroupBy(x => x.Descriptor.Identity.ContextId, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate research context: {duplicate.Key}");
        return result;
    }
}
