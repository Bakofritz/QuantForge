using QuantForge.Bots;
using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveOrderAdmissionArchitectureChecks
{
    public static void RunAll()
    {
        FinalGateRequiresEveryBinding();
        BrokerCapabilityMustExist();
        RiskHeartbeatMustBeHealthy();
        EmergencyFlattenMustRemainEnabled();
        PersistenceDefaultsToResearchOnly();
        AuthenticationAdaptersProduceDistinctFactors();
    }

    private static void FinalGateRequiresEveryBinding()
    {
        var gate = new QuantForge.Bots.LiveOrderAdmissionGate();
        var r = gate.Evaluate(Request());
        if (!r.Allowed || r.Code != "LIVE_ORDER_ADMITTED") throw new InvalidOperationException("Fully satisfied live-order gate did not admit.");
        r = gate.Evaluate(Request(recoveryClear: false));
        if (r.Allowed || r.Code != "RECOVERY_STATE_UNRESOLVED") throw new InvalidOperationException("Unresolved recovery state bypassed final gate.");
    }

    private static void BrokerCapabilityMustExist()
    {
        var gate = new QuantForge.Bots.LiveOrderAdmissionGate();
        var r = gate.Evaluate(Request(brokerCaps: new StaticBrokerCapabilityDiscovery("b", "c", Array.Empty<LiveBrokerCapability>()).Discover(DateTimeOffset.UtcNow)));
        if (r.Allowed || r.Code != "BROKER_ORDER_CAPABILITY_MISSING") throw new InvalidOperationException("Broker capability discovery did not gate order submission.");
    }

    private static void RiskHeartbeatMustBeHealthy()
    {
        var gate = new QuantForge.Bots.LiveOrderAdmissionGate();
        var stale = new LiveRiskHealthSnapshot(false, DateTimeOffset.UtcNow, "risk", "RISK_HEARTBEAT_STALE", DateTimeOffset.UtcNow.AddMinutes(-5));
        var r = gate.Evaluate(Request(risk: stale));
        if (r.Allowed || r.Code != "RISK_HEALTH_BLOCKED") throw new InvalidOperationException("Stale risk health bypassed final gate.");
    }

    private static void EmergencyFlattenMustRemainEnabled()
    {
        var gate = new QuantForge.Bots.LiveOrderAdmissionGate();
        var m = new LiveBotManifest("bot", new HashSet<LiveBotCapability> { LiveBotCapability.SendOrders }, new HashSet<LiveBotCapability>(), "bot");
        var r = gate.Evaluate(Request(bot: m));
        if (r.Allowed || r.Code != "EMERGENCY_CONTROL_REQUIRED") throw new InvalidOperationException("Emergency flatten control was not mandatory.");
    }

    private static void PersistenceDefaultsToResearchOnly()
    {
        var store = new MemoryStore();
        var manager = new LiveSecuritySettingsManager(store);
        if (manager.Current.PathwayEnabled) throw new InvalidOperationException("Persisted live pathway defaulted enabled.");
        if (manager.Current.CommunicationSettings.Enabled.Count != 0) throw new InvalidOperationException("Persisted default communications were not research-only.");
    }

    private static void AuthenticationAdaptersProduceDistinctFactors()
    {
        var now = DateTimeOffset.UtcNow;
        var a = new DeviceBoundAuthenticatorAdapter().Verify(new DeviceBoundAuthenticatorAdapter().CreateChallenge(now), "platform-proof", now);
        var b = new IndependentSecondFactorAdapter().Verify(new IndependentSecondFactorAdapter().CreateChallenge(now), "second-proof", now);
        if (!a.Succeeded || !b.Succeeded || a.Proof!.Kind == b.Proof!.Kind) throw new InvalidOperationException("Authentication adapters did not produce independent factor proofs.");
    }

    private static QuantForge.Bots.LiveOrderAdmissionRequest Request(bool recoveryClear = true, LiveBotManifest? bot = null, QuantForge.Governance.LiveBrokerCapabilitySnapshot? brokerCaps = null, QuantForge.Governance.LiveRiskHealthSnapshot? risk = null)
    {
        var now = DateTimeOffset.UtcNow;
        var settings = new LiveCommunicationSettings(
            new HashSet<LiveCommunicationCapability> { LiveCommunicationCapability.BrokerConnection, LiveCommunicationCapability.OrderRouting, LiveCommunicationCapability.RiskAlerts },
            Enum.GetValues<LiveCommunicationCapability>().Except(new[] { LiveCommunicationCapability.BrokerConnection, LiveCommunicationCapability.OrderRouting, LiveCommunicationCapability.RiskAlerts }).ToHashSet(), "live");
        return new QuantForge.Bots.LiveOrderAdmissionRequest(
            new LiveTradingSession("s", now, now.AddMinutes(5), true, "live", "auth"), now,
            "strategy", "risk", "runtime", "strategy", bot ?? new LiveBotManifest("bot", new HashSet<LiveBotCapability> { LiveBotCapability.SendOrders, LiveBotCapability.EmergencyFlatten }, new HashSet<LiveBotCapability>(), "bot"),
            settings,
            brokerCaps ?? new StaticBrokerCapabilityDiscovery("b", "c", Enum.GetValues<LiveBrokerCapability>()).Discover(now),
            risk ?? new LiveRiskHealthSnapshot(true, now, "risk", "RISK_HEALTHY", now), recoveryClear, true);
    }

    private sealed class MemoryStore : ILiveSecuritySettingsStore
    {
        private PersistedLiveSecuritySettings? _value;
        public PersistedLiveSecuritySettings? Load() => _value;
        public void Save(PersistedLiveSecuritySettings settings) => _value = settings;
    }
}
