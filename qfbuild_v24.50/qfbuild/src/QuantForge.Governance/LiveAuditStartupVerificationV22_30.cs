namespace QuantForge.Governance;

public sealed record AuditStartupVerificationV22_30(bool Healthy, string Code, string LastVerifiedEventId);

public interface IAuditChainVerifierV22_30
{
    AuditStartupVerificationV22_30 Verify();
}

public sealed class UnavailableAuditChainVerifierV22_30 : IAuditChainVerifierV22_30
{
    public AuditStartupVerificationV22_30 Verify() => new(false, "AUDIT_CHAIN_VERIFIER_UNAVAILABLE", string.Empty);
}

public static class AuditStartupGateV22_30
{
    public static bool CanEnterLiveRecovery(AuditStartupVerificationV22_30 result) =>
        result.Healthy && !string.IsNullOrWhiteSpace(result.LastVerifiedEventId);
}
