using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public enum PlatformCredentialAvailability { Unavailable, Available, Failed }

public sealed record PlatformCredentialStatus(PlatformCredentialAvailability Availability, string Provider, string Code, string Reason);

public interface IPlatformSecureKeyProviderV21_85
{
    PlatformCredentialStatus Status();
    byte[] Sign(ReadOnlySpan<byte> payload);
}

public sealed class UnavailablePlatformSecureKeyProviderV21_85 : IPlatformSecureKeyProviderV21_85
{
    public PlatformCredentialStatus Status() => new(PlatformCredentialAvailability.Unavailable, "none", "PLATFORM_SECURE_KEY_UNAVAILABLE", "No native secure-key provider has been configured.");
    public byte[] Sign(ReadOnlySpan<byte> payload) => throw new InvalidOperationException("PLATFORM_SECURE_KEY_UNAVAILABLE");
}

/// <summary>Deterministic test-only key provider. Production adapters must use OS-backed non-exportable keys.</summary>
public sealed class EphemeralTestKeyProviderV21_85 : IPlatformSecureKeyProviderV21_85
{
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    public PlatformCredentialStatus Status() => new(PlatformCredentialAvailability.Available, "ephemeral-test", "TEST_ONLY", "Ephemeral key is valid only for architecture tests; not a production credential store.");
    public byte[] Sign(ReadOnlySpan<byte> payload) => HMACSHA256.HashData(_key, payload);
}

public sealed record AuthenticatedBrokerRequestV21_85(string RequestId, string IdempotencyKey, string CanonicalPayload, string RequestFingerprint, string AuthenticationProof, DateTimeOffset CreatedAt);
public sealed record BrokerTransportAdmissionV21_85(bool Allowed, string Code, string Reason, string RequestFingerprint);

public sealed class AuthenticatedBrokerTransportBoundaryV21_85
{
    private readonly IPlatformSecureKeyProviderV21_85 _keyProvider;
    public AuthenticatedBrokerTransportBoundaryV21_85(IPlatformSecureKeyProviderV21_85 keyProvider) => _keyProvider = keyProvider;

    public AuthenticatedBrokerRequestV21_85 Prepare(LiveBrokerTransportRequest request)
    {
        var status = _keyProvider.Status();
        if (status.Availability != PlatformCredentialAvailability.Available) throw new InvalidOperationException(status.Code);
        var bytes = Encoding.UTF8.GetBytes(request.RequestFingerprint + "|" + request.IdempotencyKey + "|" + request.CanonicalPayload);
        var proof = Convert.ToHexString(_keyProvider.Sign(bytes)).ToLowerInvariant();
        return new(request.RequestId, request.IdempotencyKey, request.CanonicalPayload, request.RequestFingerprint, proof, request.CreatedAt);
    }
}

public sealed class BrokerTransportReconnectPolicyV21_85
{
    public TimeSpan ReconnectDelay { get; }
    public int MaxAttempts { get; }
    public BrokerTransportReconnectPolicyV21_85(TimeSpan? reconnectDelay = null, int maxAttempts = 3)
    { ReconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(2); MaxAttempts = maxAttempts; }

    public bool ShouldReconnect(LiveBrokerTransportHealth health, int attempts)
        => attempts < MaxAttempts && (health.State == LiveBrokerConnectionState.Degraded || health.State == LiveBrokerConnectionState.Failed || health.State == LiveBrokerConnectionState.Disconnected);
}
