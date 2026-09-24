namespace QuantForge.Governance;
public sealed record LiveStabilityStateV22_80(bool SecureRuntime,bool StartupIntegrity,bool AuditCurrent,bool BrokerSessionValid,bool BrokerHeartbeatHealthy,bool RiskHealthy,bool RecoveryCleared,bool AuthorityValid,bool LiveArmed,bool EvidenceComplete);
public static class LiveStabilityGateV22_80 { public static bool CanContinue(LiveStabilityStateV22_80 s)=>s.SecureRuntime&&s.StartupIntegrity&&s.AuditCurrent&&s.BrokerSessionValid&&s.BrokerHeartbeatHealthy&&s.RiskHealthy&&s.RecoveryCleared&&s.AuthorityValid&&s.LiveArmed&&s.EvidenceComplete; }
