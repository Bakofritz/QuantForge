namespace QuantForge.Bots;

public enum LiveBotCapability
{
    ReadLiveMarketData,
    ReadAccountState,
    ReadPositions,
    ReceiveExecutionReports,
    SendOrders,
    CancelOrders,
    EmergencyFlatten
}

public sealed record LiveBotManifest(
    string BotId,
    IReadOnlySet<LiveBotCapability> EnabledCapabilities,
    IReadOnlySet<LiveBotCapability> DeniedCapabilities,
    string ManifestFingerprint);

public sealed record LiveBotArmResult(bool Allowed, string Code, string Reason);

/// <summary>Final bot-level gate. A bot cannot send orders merely because it is registered.</summary>
public sealed class LiveBotArmGate
{
    public LiveBotArmResult Evaluate(LiveBotManifest manifest, bool liveSessionArmed, bool orderRoutingEnabled, bool riskControlsHealthy)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!liveSessionArmed) return new(false, "LIVE_SESSION_REQUIRED", "A valid live trading session is required.");
        if (!orderRoutingEnabled) return new(false, "ORDER_ROUTING_DISABLED", "Order routing is disabled by live communication settings.");
        if (!riskControlsHealthy) return new(false, "RISK_CONTROLS_UNHEALTHY", "Risk controls must be healthy before a trade bot can arm.");
        if (!manifest.EnabledCapabilities.Contains(LiveBotCapability.SendOrders)) return new(false, "ORDER_CAPABILITY_NOT_GRANTED", "The bot does not have explicit order capability.");
        if (!manifest.DeniedCapabilities.Contains(LiveBotCapability.EmergencyFlatten)) return new(false, "EMERGENCY_CONTROL_REQUIRED", "Emergency flatten capability must remain explicitly retained.");
        return new(true, "BOT_ARM_ELIGIBLE", "Bot satisfies the live-session, communication, risk, and capability gates.");
    }
}
