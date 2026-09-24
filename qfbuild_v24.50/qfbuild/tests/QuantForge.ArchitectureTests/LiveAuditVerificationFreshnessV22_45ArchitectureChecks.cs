using QuantForge.Governance;
namespace QuantForge.ArchitectureTests;
public static class LiveAuditVerificationFreshnessV22_45ArchitectureChecks
{ public static void RejectsReplayAgainstDifferentProvenance(){var v=new LiveAuditVerificationFreshnessV22_45("id","head","old",DateTimeOffset.UtcNow);if(LiveAuditVerificationFreshnessGateV22_45.IsValid(v,"new",DateTimeOffset.UtcNow,TimeSpan.FromMinutes(5)))throw new InvalidOperationException("AUDIT_REPLAY_ACCEPTED");} }
