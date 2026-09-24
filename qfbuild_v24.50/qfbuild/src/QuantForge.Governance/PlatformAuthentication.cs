namespace QuantForge.Governance;

public enum PlatformAuthenticatorKind
{
    DeviceBoundPasskey,
    DeviceBoundSecureHardware,
    IndependentSecondFactor
}

public sealed record PlatformAuthenticationChallenge(
    string ChallengeId,
    PlatformAuthenticatorKind Kind,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string RelyingPartyBinding,
    string UserBinding);

public sealed record PlatformAuthenticationProof(
    string ChallengeId,
    PlatformAuthenticatorKind Kind,
    DateTimeOffset VerifiedAt,
    string DeviceBindingFingerprint,
    string ProofFingerprint);

public interface IPlatformAuthenticationAdapter
{
    PlatformAuthenticatorKind Kind { get; }
    PlatformAuthenticationChallenge CreateChallenge(string userId, DateTimeOffset now);
    bool Verify(PlatformAuthenticationChallenge challenge, PlatformAuthenticationProof proof, DateTimeOffset now);
}

/// <summary>Platform-neutral contract. Android/Windows implementations bind this to native secure authentication later.</summary>
public sealed class DeferredPlatformAuthenticationAdapter : IPlatformAuthenticationAdapter
{
    public DeferredPlatformAuthenticationAdapter(PlatformAuthenticatorKind kind) => Kind = kind;
    public PlatformAuthenticatorKind Kind { get; }
    public PlatformAuthenticationChallenge CreateChallenge(string userId, DateTimeOffset now) =>
        new($"auth-{Guid.NewGuid():N}", Kind, now, now.AddMinutes(5), "quantforge-live-security", userId);
    public bool Verify(PlatformAuthenticationChallenge challenge, PlatformAuthenticationProof proof, DateTimeOffset now) =>
        challenge.Kind == Kind && proof.Kind == Kind && proof.ChallengeId == challenge.ChallengeId && now <= challenge.ExpiresAt && proof.VerifiedAt <= now;
}

public sealed class PlatformAuthenticationCoordinator
{
    private readonly IPlatformAuthenticationAdapter _device;
    private readonly IPlatformAuthenticationAdapter _independent;
    public PlatformAuthenticationCoordinator(IPlatformAuthenticationAdapter device, IPlatformAuthenticationAdapter independent)
    { _device = device; _independent = independent; }

    public bool VerifyBoth(string userId, PlatformAuthenticationChallenge deviceChallenge, PlatformAuthenticationProof deviceProof,
        PlatformAuthenticationChallenge independentChallenge, PlatformAuthenticationProof independentProof, DateTimeOffset now) =>
        deviceChallenge.UserBinding == userId && independentChallenge.UserBinding == userId &&
        _device.Verify(deviceChallenge, deviceProof, now) && _independent.Verify(independentChallenge, independentProof, now) &&
        deviceProof.DeviceBindingFingerprint != independentProof.DeviceBindingFingerprint;
}
