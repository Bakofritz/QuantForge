using QuantForge.Governance;
using Xunit;

namespace QuantForge.ArchitectureTests;

public sealed class LiveEnvironmentV22_30ArchitectureChecks
{
    [Fact]
    public void UnavailableBrokerSessionFailsClosed() =>
        Assert.False(new UnavailableAuthenticatedBrokerSessionV22_30().Authenticate().Authenticated);

    [Fact]
    public void BrokerIdentityMustMatchAuthority() {
        var r = new BrokerSessionResultV22_30(BrokerSessionStateV22_30.Authenticated, true, "OK",
            new("broker", "acct", "session", "authority-a"));
        Assert.False(BrokerSessionIdentityGateV22_30.IsBound(r, "authority-b"));
        Assert.True(BrokerSessionIdentityGateV22_30.IsBound(r, "authority-a"));
    }

    [Fact]
    public void ExternalBrokerOrderBindingRequiresAllIdentities() {
        var b = new BrokerOrderIdentityBindingV22_30("e", "i", "r", "broker-order", "session");
        Assert.True(BrokerOrderIdentityBindingV22_30Policy.IsComplete(b));
        Assert.False(BrokerOrderIdentityBindingV22_30Policy.IsComplete(b with { ExternalBrokerOrderId = "" }));
    }

    [Fact]
    public void AuditVerifierUnavailableBlocksLiveRecovery() =>
        Assert.False(AuditStartupGateV22_30.CanEnterLiveRecovery(new UnavailableAuditChainVerifierV22_30().Verify()));

    [Fact]
    public void LiveRoutingRequiresEveryGate() {
        var r = LiveEnvironmentStateEvaluatorV22_30.Evaluate(true, true, true, true, true, true, true, true, true, true);
        Assert.Equal(LiveEnvironmentStateV22_30.OrderRoutingAvailable, r.State);
        Assert.True(r.OrderRouting);
    }

    [Fact]
    public void DisarmedPathwayStillAllowsObservationButNotRouting() {
        var r = LiveEnvironmentStateEvaluatorV22_30.Evaluate(true, true, true, true, true, false, true, true, true, true);
        Assert.Equal(LiveEnvironmentStateV22_30.LivePathwayDisarmed, r.State);
        Assert.True(r.ReadLiveMarketData);
        Assert.False(r.OrderRouting);
    }
}
