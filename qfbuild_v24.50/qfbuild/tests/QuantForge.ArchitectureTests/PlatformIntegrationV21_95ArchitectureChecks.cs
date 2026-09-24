using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class PlatformIntegrationV21_95ArchitectureChecks
{
    public static void Run()
    {
        var android = new AndroidNativeAdapterBoundaryV21_95();
        if (android.Status.Available) throw new InvalidOperationException("Android native adapter must not claim availability before runtime binding.");
        var windows = new WindowsNativeAdapterBoundaryV21_95();
        if (windows.Status.Available) throw new InvalidOperationException("Windows native adapter must not claim availability before runtime binding.");

        var normalized = LiveBrokerOrderNormalizerV21_95.Normalize(new CanonicalLiveOrderV21_95(" es ", "buy", 1m, " limit ", 100m, null, "DAY"));
        if (normalized.Order.Symbol != "ES" || normalized.Order.Side != "BUY" || normalized.Fingerprint.Length != 64) throw new InvalidOperationException("Deterministic order normalization failed.");

        var coordinator = new LiveBrokerRecoveryCoordinatorV21_95(new BrokerTransportReconnectPolicyV21_85(maxAttempts: 1));
        var disconnected = new LiveBrokerTransportHealth(LiveBrokerConnectionState.Disconnected, DateTimeOffset.UtcNow, null, "x", "DISCONNECTED", "offline");
        if (!coordinator.Evaluate(disconnected, 0).Blocked) throw new InvalidOperationException("Disconnected transport must block execution.");
        var connected = disconnected with { State = LiveBrokerConnectionState.Connected, LastHeartbeatAt = DateTimeOffset.UtcNow };
        if (!coordinator.Evaluate(connected, 1).Blocked) throw new InvalidOperationException("Unknown execution intent must block execution.");
        if (coordinator.Evaluate(connected, 0).Blocked) throw new InvalidOperationException("Clear recovery state should reach final authority recheck.");
        if (!coordinator.CanReconnect(disconnected)) throw new InvalidOperationException("Reconnect policy should permit bounded recovery attempt.");
        coordinator.RecordReconnectAttempt();
        if (coordinator.CanReconnect(disconnected)) throw new InvalidOperationException("Reconnect attempts must be bounded.");
    }
}
