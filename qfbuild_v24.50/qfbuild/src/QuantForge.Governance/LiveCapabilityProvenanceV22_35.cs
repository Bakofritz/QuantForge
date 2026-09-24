using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public sealed record LiveCapabilityProvenanceV22_35(
    string PlatformFingerprint,
    string BrokerSessionId,
    string AuthorityFingerprint,
    string AuditVerificationEventId,
    string RiskSnapshotFingerprint,
    bool RecoveryCleared,
    bool LivePathwayArmed);

public static class LiveCapabilityProvenanceV22_35Policy
{
    public static bool IsComplete(LiveCapabilityProvenanceV22_35 p) =>
        !string.IsNullOrWhiteSpace(p.PlatformFingerprint) &&
        !string.IsNullOrWhiteSpace(p.BrokerSessionId) &&
        !string.IsNullOrWhiteSpace(p.AuthorityFingerprint) &&
        !string.IsNullOrWhiteSpace(p.AuditVerificationEventId) &&
        !string.IsNullOrWhiteSpace(p.RiskSnapshotFingerprint);

    public static string ComputeFingerprint(LiveCapabilityProvenanceV22_35 p)
    {
        var canonical = string.Join("|", p.PlatformFingerprint, p.BrokerSessionId,
            p.AuthorityFingerprint, p.AuditVerificationEventId,
            p.RiskSnapshotFingerprint, p.RecoveryCleared, p.LivePathwayArmed);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
