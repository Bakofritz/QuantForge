namespace QuantForge.Governance;

public sealed record LiveAuthorityRevalidationV22_35(
    bool PlatformMatches,
    bool BrokerSessionMatches,
    bool AuthorityMatches,
    bool AuditStillHealthy,
    bool RiskStillHealthy,
    bool RecoveryStillCleared,
    bool PathwayStillArmed);

public static class LiveAuthorityRevalidationGateV22_35
{
    public static bool CanContinue(LiveAuthorityRevalidationV22_35 r) =>
        r.PlatformMatches && r.BrokerSessionMatches && r.AuthorityMatches &&
        r.AuditStillHealthy && r.RiskStillHealthy && r.RecoveryStillCleared &&
        r.PathwayStillArmed;
}
