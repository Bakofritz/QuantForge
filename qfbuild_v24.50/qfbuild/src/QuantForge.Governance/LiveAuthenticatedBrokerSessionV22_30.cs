namespace QuantForge.Governance;

public enum BrokerSessionStateV22_30 { Disconnected, Authenticating, Authenticated, Expired, Faulted }

public sealed record BrokerSessionIdentityV22_30(
    string BrokerName,
    string AccountIdentity,
    string SessionId,
    string AuthorityFingerprint);

public sealed record BrokerSessionResultV22_30(
    BrokerSessionStateV22_30 State,
    bool Authenticated,
    string Code,
    BrokerSessionIdentityV22_30? Identity);

/// <summary>Authenticated broker-session contract. Transport implementations remain outside the governance assembly.</summary>
public interface IAuthenticatedBrokerSessionV22_30
{
    BrokerSessionResultV22_30 Authenticate();
    BrokerSessionResultV22_30 Refresh();
    void Disconnect();
}

public sealed class UnavailableAuthenticatedBrokerSessionV22_30 : IAuthenticatedBrokerSessionV22_30
{
    public BrokerSessionResultV22_30 Authenticate() => Blocked();
    public BrokerSessionResultV22_30 Refresh() => Blocked();
    public void Disconnect() { }
    private static BrokerSessionResultV22_30 Blocked() => new(BrokerSessionStateV22_30.Disconnected, false, "BROKER_AUTHENTICATED_SESSION_UNAVAILABLE", null);
}

public static class BrokerSessionIdentityGateV22_30
{
    public static bool IsBound(BrokerSessionResultV22_30 result, string expectedAuthorityFingerprint) =>
        result.Authenticated && result.State == BrokerSessionStateV22_30.Authenticated &&
        result.Identity is not null &&
        !string.IsNullOrWhiteSpace(result.Identity.SessionId) &&
        result.Identity.AuthorityFingerprint == expectedAuthorityFingerprint;
}
