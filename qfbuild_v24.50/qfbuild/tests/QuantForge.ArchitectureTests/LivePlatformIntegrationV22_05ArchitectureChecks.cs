using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LivePlatformIntegrationV22_05ArchitectureChecks
{
    public static void Run()
    {
        var platform = new UnavailablePlatformSecurityRuntimeV22_05();
        var denied = PlatformSecurityReadinessV22_05.RequireAvailable(platform, PlatformSecureOperationV22_05.Authenticate, ReadOnlySpan<byte>.Empty);
        if (denied.Success || denied.Code != "PLATFORM_SECURITY_UNAVAILABLE") throw new InvalidOperationException("Native platform security must fail closed.");

        var broker = new UnavailableBrokerConnectorV22_05();
        if (broker.State != LiveBrokerConnectionState.Disconnected) throw new InvalidOperationException("Default broker connector must be disconnected.");

        var startup = new LiveStartupIntegrityCoordinatorV22_05();
        var blocked = startup.Evaluate(false, true, new LiveExecutionRecoveryStateV21_95(false, "READY", "", 0, LiveBrokerConnectionState.Connected));
        if (blocked.Ready) throw new InvalidOperationException("Invalid audit chain must block startup readiness.");
    }
}
