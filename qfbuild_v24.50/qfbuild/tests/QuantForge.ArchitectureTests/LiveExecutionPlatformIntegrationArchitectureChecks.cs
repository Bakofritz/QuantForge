using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveExecutionPlatformIntegrationArchitectureChecks
{
    public static void Run()
    {
        NativeAuthenticationMustFailClosedWhenUnavailable();
        SecureKeyStoreMustFailClosedWhenUnavailable();
        CanonicalBrokerRequestMustBeDeterministic();
        SqliteIntentStoreMustSurviveReopen();
        RecoveryGateMustBlockUnknownIntents();
    }

    private static void NativeAuthenticationMustFailClosedWhenUnavailable()
    {
        var adapter = new DeviceBoundAuthenticatorAdapter();
        var bridge = new NativeAuthenticationBridge("Android", adapter);
        var challenge = adapter.CreateChallenge(DateTimeOffset.UtcNow);
        var result = bridge.Verify(challenge, "proof", DateTimeOffset.UtcNow);
        if (result.Succeeded || result.Code != "NATIVE_AUTH_UNAVAILABLE") throw new InvalidOperationException("Native auth availability gate failed.");
    }

    private static void SecureKeyStoreMustFailClosedWhenUnavailable()
    {
        var store = new UnsupportedNativeSecureKeyStore();
        try { store.Retrieve("live-key"); throw new InvalidOperationException("Unavailable secure key store did not fail closed."); }
        catch (PlatformNotSupportedException) { }
    }

    private static void CanonicalBrokerRequestMustBeDeterministic()
    {
        var request = new LiveBrokerSubmissionRequest("k", "p", "of", "sf", DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var a = LiveBrokerRequestSerializer.Serialize(request).RequestFingerprint();
        var b = LiveBrokerRequestSerializer.Serialize(request).RequestFingerprint();
        if (a != b) throw new InvalidOperationException("Broker request fingerprint is not deterministic.");
    }

    private static void SqliteIntentStoreMustSurviveReopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qf-live-{Guid.NewGuid():N}.db");
        try
        {
            var intent = new LiveExecutionIntent("i", "k", "p", "pf", "sf", "of", DateTimeOffset.UtcNow, LiveExecutionIntentState.Unknown, null, null);
            using (var first = new SqliteLiveExecutionIntentStore(path)) first.Save(intent);
            using (var second = new SqliteLiveExecutionIntentStore(path))
                if (second.GetByIdempotencyKey("k")?.State != LiveExecutionIntentState.Unknown) throw new InvalidOperationException("SQLite execution intent did not survive reopen.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void RecoveryGateMustBlockUnknownIntents()
    {
        var store = new InMemoryLiveExecutionIntentStore();
        store.Save(new LiveExecutionIntent("i", "k", "p", "pf", "sf", "of", DateTimeOffset.UtcNow, LiveExecutionIntentState.Unknown, null, null));
        var decision = new LiveExecutionRecoveryGate(store).Inspect(new[] { "k" });
        if (decision.SafeToProceed || decision.UnknownIntentCount != 1) throw new InvalidOperationException("Recovery gate failed to block ambiguous execution.");
    }
}
