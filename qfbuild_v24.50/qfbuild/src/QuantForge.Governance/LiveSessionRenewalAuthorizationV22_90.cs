namespace QuantForge.Governance;

public sealed record LiveSessionRenewalAuthorizationV22_90(
    string SessionId,
    string AuthorityFingerprint,
    string PriorLeaseFingerprint,
    DateTime RequestedAtUtc,
    DateTime ExpiresAtUtc)
{
    public bool IsValid(string expectedSessionId, string expectedAuthorityFingerprint, string expectedPriorLeaseFingerprint, DateTime nowUtc) =>
        !string.IsNullOrWhiteSpace(SessionId) &&
        SessionId == expectedSessionId &&
        AuthorityFingerprint == expectedAuthorityFingerprint &&
        PriorLeaseFingerprint == expectedPriorLeaseFingerprint &&
        ExpiresAtUtc > RequestedAtUtc &&
        nowUtc <= ExpiresAtUtc;
}

public static class LiveSessionRenewalAuthorizationGateV22_90
{
    public static bool CanRenew(LiveSessionRenewalAuthorizationV22_90 request, string sessionId, string authorityFingerprint, string priorLeaseFingerprint, DateTime nowUtc) =>
        request is not null && request.IsValid(sessionId, authorityFingerprint, priorLeaseFingerprint, nowUtc);
}
