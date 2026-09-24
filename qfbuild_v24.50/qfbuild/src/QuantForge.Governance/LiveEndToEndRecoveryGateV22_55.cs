namespace QuantForge.Governance;

public enum LiveRecoveryStageV22_55 { Prepared, Submitted, Unknown, ReconcileRequired, Reconciled, Revalidated, Blocked }
public sealed record LiveEndToEndRecoveryStateV22_55(LiveRecoveryStageV22_55 Stage,bool BrokerConnected,bool AuditHealthy,bool RiskHealthy,bool AuthorityValid,bool IdentityBound);
public static class LiveEndToEndRecoveryGateV22_55
{
 public static bool CanResume(LiveEndToEndRecoveryStateV22_55 s) => s.Stage==LiveRecoveryStageV22_55.Revalidated&&s.BrokerConnected&&s.AuditHealthy&&s.RiskHealthy&&s.AuthorityValid&&s.IdentityBound;
 public static bool RequiresReconciliation(LiveEndToEndRecoveryStateV22_55 s) => s.Stage==LiveRecoveryStageV22_55.Unknown||s.Stage==LiveRecoveryStageV22_55.ReconcileRequired;
}
