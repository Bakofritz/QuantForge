using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveExecutionV21_75ArchitectureChecks
{
    public static void Run()
    {
        var lifecycle = new LiveBrokerConnectionLifecycle();
        var now = DateTimeOffset.UtcNow;
        if (lifecycle.Snapshot().State != LiveBrokerConnectionState.Disconnected) throw new Exception("Default broker transport must be disconnected.");
        lifecycle.BeginConnect("connection-fp", now);
        if (lifecycle.Snapshot().State != LiveBrokerConnectionState.Connecting) throw new Exception("Connect transition failed.");
        lifecycle.MarkConnected(now);
        if (lifecycle.Snapshot().State != LiveBrokerConnectionState.Connected) throw new Exception("Connected transition failed.");
        lifecycle.MarkDegraded("heartbeat stale", now);
        if (lifecycle.Snapshot().State != LiveBrokerConnectionState.Degraded) throw new Exception("Degraded transition failed.");
        lifecycle.Disconnect(now);
        if (lifecycle.Snapshot().State != LiveBrokerConnectionState.Disconnected) throw new Exception("Disconnect transition failed.");

        var transport = new UnconfiguredLiveBrokerTransport();
        if (transport.Health(now).State != LiveBrokerConnectionState.Disconnected) throw new Exception("Unconfigured transport must fail closed.");
        try { transport.Send(new("r", "i", "{}", "fp", now)); throw new Exception("Unconfigured transport must reject send."); } catch (InvalidOperationException ex) when (ex.Message == "BROKER_TRANSPORT_UNCONFIGURED") { }

        var android = new AndroidKeystoreAdapter();
        if (android.IsAvailable) throw new Exception("Android adapter must default unavailable.");
        var windows = new WindowsProtectedKeyStoreAdapter();
        if (windows.IsAvailable) throw new Exception("Windows adapter must default unavailable.");
    }
}
