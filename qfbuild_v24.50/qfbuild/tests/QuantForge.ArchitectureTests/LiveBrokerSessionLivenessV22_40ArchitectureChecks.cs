using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveBrokerSessionLivenessV22_40ArchitectureChecks
{
    public static void RejectsStaleSession()
    {
        var s = new LiveBrokerSessionLivenessV22_40("session", DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(-1), true);
        if (LiveBrokerSessionLivenessGateV22_40.IsFresh(s, DateTimeOffset.UtcNow)) throw new InvalidOperationException("STALE_SESSION_ACCEPTED");
    }
}
