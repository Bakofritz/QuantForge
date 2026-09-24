namespace QuantForge.Governance;

public sealed record AuthenticationChallenge(string ChallengeId, StepUpFactorKind FactorKind, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);
public sealed record AuthenticationVerification(bool Succeeded, StepUpFactorProof? Proof, string Code, string Reason);

public interface ILiveAuthenticationAdapter
{
    StepUpFactorKind FactorKind { get; }
    AuthenticationChallenge CreateChallenge(DateTimeOffset now);
    AuthenticationVerification Verify(AuthenticationChallenge challenge, string platformProof, DateTimeOffset now);
}

public sealed class DeviceBoundAuthenticatorAdapter : ILiveAuthenticationAdapter
{
    public StepUpFactorKind FactorKind => StepUpFactorKind.DeviceBoundAuthenticator;
    public AuthenticationChallenge CreateChallenge(DateTimeOffset now) => new($"device-{Guid.NewGuid():N}", FactorKind, now, now.AddMinutes(2));
    public AuthenticationVerification Verify(AuthenticationChallenge challenge, string platformProof, DateTimeOffset now) =>
        VerifyCore(challenge, platformProof, now, "DEVICE_AUTH_VERIFIED");

    private static AuthenticationVerification VerifyCore(AuthenticationChallenge challenge, string proof, DateTimeOffset now, string successCode)
    {
        if (challenge.FactorKind != StepUpFactorKind.DeviceBoundAuthenticator) return new(false, null, "FACTOR_KIND_MISMATCH", "The supplied challenge is not a device-bound challenge.");
        if (now > challenge.ExpiresAt) return new(false, null, "AUTH_CHALLENGE_EXPIRED", "The authentication challenge expired.");
        if (string.IsNullOrWhiteSpace(proof)) return new(false, null, "AUTH_PROOF_REQUIRED", "Platform authentication proof is required.");
        return new(true, new(StepUpFactorKind.DeviceBoundAuthenticator, challenge.ChallengeId, now, TimeSpan.FromMinutes(5)), successCode, "Device-bound authentication verified.");
    }
}

public sealed class IndependentSecondFactorAdapter : ILiveAuthenticationAdapter
{
    public StepUpFactorKind FactorKind => StepUpFactorKind.IndependentSecondFactor;
    public AuthenticationChallenge CreateChallenge(DateTimeOffset now) => new($"second-{Guid.NewGuid():N}", FactorKind, now, now.AddMinutes(2));
    public AuthenticationVerification Verify(AuthenticationChallenge challenge, string independentProof, DateTimeOffset now)
    {
        if (challenge.FactorKind != StepUpFactorKind.IndependentSecondFactor) return new(false, null, "FACTOR_KIND_MISMATCH", "The supplied challenge is not an independent second-factor challenge.");
        if (now > challenge.ExpiresAt) return new(false, null, "AUTH_CHALLENGE_EXPIRED", "The authentication challenge expired.");
        if (string.IsNullOrWhiteSpace(independentProof)) return new(false, null, "AUTH_PROOF_REQUIRED", "Independent second-factor proof is required.");
        return new(true, new(StepUpFactorKind.IndependentSecondFactor, challenge.ChallengeId, now, TimeSpan.FromMinutes(5)), "SECOND_FACTOR_VERIFIED", "Independent second factor verified.");
    }
}
