namespace QuantForge.Governance;

public sealed record LiveBrokerSessionLivenessV22_40(
    string SessionId, DateTimeOffset ObservedAtUtc, DateTimeOffset ExpiresAtUtc, bool Connected);

public static class LiveBrokerSessionLivenessGateV22_40
{
    public static bool IsFresh(LiveBrokerSessionLivenessV22_40 s, DateTimeOffset nowUtc) =>
        !string.IsNullOrWhiteSpace(s.SessionId) && s.Connected && nowUtc <= s.ExpiresAtUtc && s.ExpiresAtUtc > s.ObservedAtUtc;
}
