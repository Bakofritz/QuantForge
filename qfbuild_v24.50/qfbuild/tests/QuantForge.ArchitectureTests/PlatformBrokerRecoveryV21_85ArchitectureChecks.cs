using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class PlatformBrokerRecoveryV21_85ArchitectureChecks
{
    public static void RunAll()
    {
        var unavailable = new UnavailablePlatformSecureKeyProviderV21_85();
        if (unavailable.Status().Availability != PlatformCredentialAvailability.Unavailable) throw new Exception("Secure key default must be unavailable.");
        try { unavailable.Sign(new byte[] { 1 }); throw new Exception("Unavailable key provider must fail closed."); } catch (InvalidOperationException) { }

        var testKey = new EphemeralTestKeyProviderV21_85();
        var boundary = new AuthenticatedBrokerTransportBoundaryV21_85(testKey);
        var request = new LiveBrokerTransportRequest("req", "idem", "payload", "fp", DateTimeOffset.UtcNow);
        var authenticated = boundary.Prepare(request);
        if (string.IsNullOrWhiteSpace(authenticated.AuthenticationProof)) throw new Exception("Authenticated proof missing.");
        if (authenticated.IdempotencyKey != request.IdempotencyKey) throw new Exception("Idempotency binding lost.");

        var policy = new BrokerTransportReconnectPolicyV21_85(maxAttempts: 2);
        var degraded = new LiveBrokerTransportHealth(LiveBrokerConnectionState.Degraded, DateTimeOffset.UtcNow, null, "x", "D", "d");
        if (!policy.ShouldReconnect(degraded, 0) || policy.ShouldReconnect(degraded, 2)) throw new Exception("Reconnect policy failed.");
    }
}
