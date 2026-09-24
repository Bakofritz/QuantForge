using QuantForge.Governance;
using Xunit;

namespace QuantForge.ArchitectureTests;

public sealed class LivePlatformBrokerV22_15ArchitectureChecks
{
    [Fact] public void SecureRuntimeFailsClosed() => Assert.False(new UnavailableNativeSecureKeyRuntimeV22_15().Status.Available);
    [Fact] public void UnknownBrokerStateIsNotDefinitive()
    {
        var r = LiveBrokerOrderStateNormalizerV22_15.Normalize(new("x", "future_state", true, "idem", "fp"));
        Assert.Equal(NormalizedLiveOrderStateV22_15.Unknown, r.State); Assert.False(r.Definitive);
    }
    [Fact] public void MissingIdentityBindingBlocksNormalization()
    {
        var r = LiveBrokerOrderStateNormalizerV22_15.Normalize(new("x", "FILLED", true, "", "fp"));
        Assert.False(r.Definitive);
    }
    [Fact] public void RecoveryBlocksUnknownExecution()
    {
        var r = new LiveExecutionReconciliationOrchestratorV22_15().Evaluate(true,true,true,true,true,true);
        Assert.False(r.CanProceed); Assert.Equal("UNKNOWN_EXECUTION_REQUIRES_RECONCILIATION", r.Code);
    }
    [Fact] public void RecoveryRequiresAuthorityRevalidation()
    {
        var r = new LiveExecutionReconciliationOrchestratorV22_15().Evaluate(true,true,true,true,false,false);
        Assert.False(r.CanProceed); Assert.Equal("AUTHORITY_REVALIDATION_FAILED", r.Code);
    }
}
