using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveTradingFinalBoundaryChecks_v21_35
{
    public static void Run()
    {
        var audit = new TamperEvidentLiveTradingAuditSink();
        var authDevice = new DeferredPlatformAuthenticationAdapter(PlatformAuthenticatorKind.DeviceBoundPasskey);
        var authSecond = new DeferredPlatformAuthenticationAdapter(PlatformAuthenticatorKind.IndependentSecondFactor);
        var coordinator = new PlatformAuthenticationCoordinator(authDevice, authSecond);
        var now = DateTimeOffset.UtcNow;
        var dc = authDevice.CreateChallenge("u", now); var sc = authSecond.CreateChallenge("u", now);
        var dp = new PlatformAuthenticationProof(dc.ChallengeId, dc.Kind, now, "device-A", "proof-A");
        var sp = new PlatformAuthenticationProof(sc.ChallengeId, sc.Kind, now, "second-B", "proof-B");
        if (!coordinator.VerifyBoth("u", dc, dp, sc, sp, now)) throw new InvalidOperationException("AUTH_COORDINATOR_FAIL");

        audit.Append(new("1", LiveTradingSecurityEvent.SettingsChanged, now, "u", "test", "fp", null, null));
        audit.Append(new("2", LiveTradingSecurityEvent.ArmRequested, now, "u", "test", "fp", null, null));
        if (!audit.ChainValid()) throw new InvalidOperationException("AUDIT_CHAIN_FAIL");

        var probe = new DeterministicBrokerCapabilityProbe("test", Enum.GetValues<BrokerCapability>());
        var broker = probe.Probe(now);
        var risk = new RiskHealthMonitor(TimeSpan.FromSeconds(10)); risk.RecordHeartbeat(now, "risk");
        var session = new LiveTradingSession("s", now, now.AddMinutes(10), true, "settings", "auth");
        var comm = new LiveCommunicationSettings(
            Enum.GetValues<LiveCommunicationCapability>().ToHashSet(),
            new HashSet<LiveCommunicationCapability>(), "all");
        var gate = new LiveOrderAdmissionGate();
        var allowed = gate.Evaluate(new(now, "u", session, comm, broker, risk.Snapshot, "strategy", "risk", "runtime", "recovery", true, true, true, true, true));
        if (!allowed.Allowed) throw new InvalidOperationException("FINAL_ADMISSION_FAIL");
        var stale = risk.Evaluate(now.AddSeconds(11));
        if (stale.State != RiskHealthState.Stale) throw new InvalidOperationException("RISK_STALE_DETECTION_FAIL");
        var blocked = gate.Evaluate(new(now.AddSeconds(11), "u", session, comm, broker, stale, "strategy", "risk", "runtime", "recovery", true, true, true, true, true));
        if (blocked.Allowed || blocked.Code != "RISK_HEALTH_NOT_HEALTHY") throw new InvalidOperationException("RISK_GATE_FAIL");
    }
}
