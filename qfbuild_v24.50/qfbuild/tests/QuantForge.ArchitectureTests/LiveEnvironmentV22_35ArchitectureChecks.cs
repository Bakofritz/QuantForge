using QuantForge.Governance;
using Xunit;

namespace QuantForge.ArchitectureTests;

public sealed class LiveEnvironmentV22_35ArchitectureChecks
{
    [Fact]
    public void CapabilityProvenanceRequiresEveryIdentity() {
        var p = new LiveCapabilityProvenanceV22_35("platform", "session", "authority", "audit", "risk", true, true);
        Assert.True(LiveCapabilityProvenanceV22_35Policy.IsComplete(p));
        Assert.False(LiveCapabilityProvenanceV22_35Policy.IsComplete(p with { BrokerSessionId = "" }));
    }

    [Fact]
    public void CapabilityFingerprintChangesWhenAuthorityChanges() {
        var p = new LiveCapabilityProvenanceV22_35("platform", "session", "authority-a", "audit", "risk", true, true);
        var changed = p with { AuthorityFingerprint = "authority-b" };
        Assert.NotEqual(LiveCapabilityProvenanceV22_35Policy.ComputeFingerprint(p), LiveCapabilityProvenanceV22_35Policy.ComputeFingerprint(changed));
    }

    [Fact]
    public void AnyRevalidationFailureBlocksContinuation() {
        var r = new LiveAuthorityRevalidationV22_35(true, true, true, true, true, true, true);
        Assert.True(LiveAuthorityRevalidationGateV22_35.CanContinue(r));
        Assert.False(LiveAuthorityRevalidationGateV22_35.CanContinue(r with { RiskStillHealthy = false }));
    }

    [Fact]
    public void DisarmedPathwayCannotContinueLiveExecution() {
        var r = new LiveAuthorityRevalidationV22_35(true, true, true, true, true, true, false);
        Assert.False(LiveAuthorityRevalidationGateV22_35.CanContinue(r));
    }
}
