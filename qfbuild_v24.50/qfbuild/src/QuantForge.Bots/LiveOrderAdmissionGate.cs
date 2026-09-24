using QuantForge.Governance;

namespace QuantForge.Bots;

public sealed record LiveOrderAdmissionRequest(
    LiveTradingSession? Session,
    DateTimeOffset RequestedAt,
    string StrategyRegistryFingerprint,
    string RiskPolicyFingerprint,
    string RuntimeQualificationFingerprint,
    string ApprovedStrategyFingerprint,
    LiveBotManifest BotManifest,
    LiveCommunicationSettings CommunicationSettings,
    LiveBrokerCapabilitySnapshot BrokerCapabilities,
    LiveRiskHealthSnapshot RiskHealth,
    bool RecoveryStateClear,
    bool SettingsCurrent);

public sealed record LiveOrderAdmissionResult(bool Allowed, string Code, string Reason);

public sealed class LiveOrderAdmissionGate
{
    public LiveOrderAdmissionResult Evaluate(LiveOrderAdmissionRequest request)
    {
        try { LiveTradingSecurityService.RequireArmedSession(request.Session, request.RequestedAt); }
        catch (Exception ex) { return Denied("SESSION_BLOCKED", ex.Message); }
        if (!request.RecoveryStateClear) return Denied("RECOVERY_STATE_UNRESOLVED", "Unresolved recovery or reconciliation state blocks live order admission.");
        if (!request.SettingsCurrent) return Denied("SETTINGS_STALE", "Live security settings changed or are stale relative to the armed session.");
        if (request.StrategyRegistryFingerprint != request.ApprovedStrategyFingerprint) return Denied("STRATEGY_BINDING_MISMATCH", "The armed strategy binding does not match the approved strategy.");
        if (request.RiskPolicyFingerprint != request.RiskHealth.RiskPolicyFingerprint) return Denied("RISK_BINDING_MISMATCH", "The risk-health binding does not match the active risk policy.");
        if (!request.RiskHealth.Healthy) return Denied("RISK_HEALTH_BLOCKED", request.RiskHealth.Reason);
        if (!request.BrokerCapabilities.Healthy) return Denied("BROKER_UNHEALTHY", "Broker capability discovery reports an unhealthy connection.");
        if (!request.BrokerCapabilities.Capabilities.Contains(LiveBrokerCapability.OrderSubmission)) return Denied("BROKER_ORDER_CAPABILITY_MISSING", "Broker does not advertise order submission capability.");
        if (!request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.OrderRouting)) return Denied("ORDER_ROUTING_DISABLED", "Order-routing communication is disabled.");
        if (!request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.BrokerConnection)) return Denied("BROKER_CONNECTION_DISABLED", "Broker communication is disabled.");
        if (!request.BotManifest.EnabledCapabilities.Contains(LiveBotCapability.SendOrders)) return Denied("BOT_ORDER_CAPABILITY_MISSING", "The bot manifest does not explicitly authorize order submission.");
        if (!request.BotManifest.EnabledCapabilities.Contains(LiveBotCapability.EmergencyFlatten)) return Denied("EMERGENCY_CONTROL_REQUIRED", "Emergency flatten capability must remain available.");
        return new(true, "LIVE_ORDER_ADMITTED", "All final live-order admission gates passed.");
    }

    private static LiveOrderAdmissionResult Denied(string code, string reason) => new(false, code, reason);
}
