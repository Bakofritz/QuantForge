namespace QuantForge.Governance;

public sealed record LiveOrderAdmissionRequest(
    DateTimeOffset RequestedAt,
    string UserId,
    LiveTradingSession? Session,
    LiveCommunicationSettings CommunicationSettings,
    BrokerCapabilityHandshakeSnapshot Broker,
    RiskHealthSnapshot RiskHealth,
    string StrategyRegistryFingerprint,
    string RiskPolicyFingerprint,
    string RuntimeQualificationFingerprint,
    string? RecoveryReconciliationFingerprint,
    bool StrategyApproved,
    bool RiskControlsHealthy,
    bool BotSendOrdersGranted,
    bool EmergencyFlattenRetained,
    bool AuditChainHealthy);

public sealed record LiveOrderAdmissionResult(bool Allowed, string Code, string Reason, IReadOnlyList<string> Checks);

/// <summary>Single final safety boundary before any future broker order-submission adapter may be called.</summary>
public sealed class LiveOrderAdmissionGate
{
    private readonly BrokerCapabilityGate _brokerGate = new();
    public LiveOrderAdmissionResult Evaluate(LiveOrderAdmissionRequest r)
    {
        var checks = new List<string>();
        void Fail(string code, string reason) => throw new AdmissionException(code, reason);
        try
        {
            LiveTradingSecurityService.RequireArmedSession(r.Session, r.RequestedAt); checks.Add("armed-session");
            if (!r.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.OrderRouting)) Fail("ORDER_ROUTING_DISABLED", "Order routing is not enabled.");
            if (!r.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.BrokerConnection)) Fail("BROKER_CONNECTION_DISABLED", "Broker communication is not enabled.");
            if (!r.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.RiskAlerts)) Fail("RISK_ALERTS_DISABLED", "Risk-alert communications are required for order routing.");
            checks.Add("communication-scope");
            var broker = _brokerGate.Require(r.Broker, BrokerCapability.OrderSubmission, BrokerCapability.OrderCancellation, BrokerCapability.ExecutionReports, BrokerCapability.Heartbeat);
            if (!broker.Allowed) Fail(broker.Code, broker.Reason); checks.Add("broker-capabilities");
            if (r.RiskHealth.State != RiskHealthState.Healthy) Fail("RISK_HEALTH_NOT_HEALTHY", "Risk heartbeat is not currently healthy."); checks.Add("risk-heartbeat");
            if (!r.StrategyApproved) Fail("STRATEGY_NOT_APPROVED", "The strategy is not approved for live use.");
            if (!r.RiskControlsHealthy) Fail("RISK_CONTROLS_UNHEALTHY", "Risk controls are not healthy.");
            if (!r.BotSendOrdersGranted) Fail("SEND_ORDERS_NOT_GRANTED", "The bot does not have explicit SendOrders authority.");
            if (!r.EmergencyFlattenRetained) Fail("EMERGENCY_CONTROL_REQUIRED", "Emergency flatten capability must remain retained.");
            if (!r.AuditChainHealthy) Fail("AUDIT_CHAIN_UNHEALTHY", "The tamper-evident audit chain is not healthy.");
            if (string.IsNullOrWhiteSpace(r.RecoveryReconciliationFingerprint)) Fail("RECOVERY_CLEARANCE_REQUIRED", "No unresolved recovery/reconciliation state may exist before live order admission.");
            if (string.IsNullOrWhiteSpace(r.StrategyRegistryFingerprint) || string.IsNullOrWhiteSpace(r.RiskPolicyFingerprint) || string.IsNullOrWhiteSpace(r.RuntimeQualificationFingerprint)) Fail("BINDINGS_REQUIRED", "Strategy, risk, and runtime bindings are required.");
            checks.Add("strategy-risk-runtime-bindings"); checks.Add("recovery-clearance"); checks.Add("emergency-control"); checks.Add("audit-integrity");
            return new(true, "LIVE_ORDER_ADMITTED", "All final live-order admission controls passed. No broker order has been submitted by this gate.", checks);
        }
        catch (AdmissionException ex) { checks.Add($"blocked:{ex.Code}"); return new(false, ex.Code, ex.Message, checks); }
    }
    private sealed class AdmissionException(string code, string message) : Exception(message) { public string Code { get; } = code; }
}
