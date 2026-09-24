namespace QuantForge.Governance;

public sealed record BrokerSessionIdentityV22_05(string BrokerId, string SessionId, string CapabilityFingerprint);
public sealed record BrokerOrderResultV22_05(string ExternalOrderId, string Status, string RequestFingerprint, string IdempotencyKey, bool Definitive);

/// <summary>Broker-specific connector contract. It communicates only after the independent live authority gate has admitted the request.</summary>
public interface IAuthenticatedBrokerConnectorV22_05
{
    BrokerSessionIdentityV22_05 Session { get; }
    LiveBrokerConnectionState State { get; }
    BrokerOrderResultV22_05 SubmitAuthorized(ReadOnlySpan<byte> canonicalRequest, string idempotencyKey, string requestFingerprint);
    BrokerOrderResultV22_05 QueryByIdempotencyKey(string idempotencyKey);
}

public sealed class UnavailableBrokerConnectorV22_05 : IAuthenticatedBrokerConnectorV22_05
{
    public BrokerSessionIdentityV22_05 Session => new("none", "none", "none");
    public LiveBrokerConnectionState State => LiveBrokerConnectionState.Disconnected;
    public BrokerOrderResultV22_05 SubmitAuthorized(ReadOnlySpan<byte> canonicalRequest, string idempotencyKey, string requestFingerprint) =>
        throw new InvalidOperationException("BROKER_CONNECTOR_UNAVAILABLE");
    public BrokerOrderResultV22_05 QueryByIdempotencyKey(string idempotencyKey) =>
        throw new InvalidOperationException("BROKER_CONNECTOR_UNAVAILABLE");
}
