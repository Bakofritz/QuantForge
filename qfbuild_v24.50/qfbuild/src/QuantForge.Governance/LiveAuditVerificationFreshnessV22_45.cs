using System.Security.Cryptography;
using System.Text;
namespace QuantForge.Governance;

public sealed record LiveAuditVerificationFreshnessV22_45(string VerificationId,string ChainHead,string ProvenanceFingerprint,DateTimeOffset VerifiedAtUtc);
public static class LiveAuditVerificationFreshnessGateV22_45
{
 public static bool IsValid(LiveAuditVerificationFreshnessV22_45 v,string expectedProvenance,DateTimeOffset nowUtc,TimeSpan maxAge) =>
 !string.IsNullOrWhiteSpace(v.VerificationId)&&!string.IsNullOrWhiteSpace(v.ChainHead)&&v.ProvenanceFingerprint==expectedProvenance&&v.VerifiedAtUtc<=nowUtc&&nowUtc-v.VerifiedAtUtc<=maxAge;
}
