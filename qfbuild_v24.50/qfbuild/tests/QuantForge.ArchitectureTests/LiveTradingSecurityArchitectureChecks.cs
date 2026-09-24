using QuantForge.Bots;
using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveTradingSecurityArchitectureChecks
{
    public static void RunAll()
    {
        DefaultPolicyIsDisabled();
        TwoIndependentStepUpFactorsAreRequired();
        FreshReauthenticationIsRequired();
        OrderRoutingRequiresBrokerAndRiskAlerts();
        PositionUpdatesRequireAccountState();
        ExpiredSessionIsRejected();
        CommunicationIsExplicitlyScoped();
        BotCannotArmWithoutLiveSession();
        BotRequiresExplicitOrderCapability();
        BotRetainsEmergencyControl();
        AuthorityPolicyRemainsFinalGate();
        PathwayControllerRequiresPrivilegedSettingChange();
        ArmedStateHasEmergencyDisarm();
    }

    private static void DefaultPolicyIsDisabled()
    {
        if (LiveTradingSecurityPolicy.Default.PathwayEnabled) throw new InvalidOperationException("Live pathway must default disabled.");
    }

    private static void TwoIndependentStepUpFactorsAreRequired()
    {
        var s = EnabledService();
        var r = s.AuthorizeArming(Request(device: null, second: Valid(StepUpFactorKind.IndependentSecondFactor)));
        if (r.Allowed || r.Code != "DEVICE_FACTOR_REQUIRED") throw new InvalidOperationException("Device-bound factor must be required.");
        r = s.AuthorizeArming(Request(device: Valid(StepUpFactorKind.DeviceBoundAuthenticator), second: null));
        if (r.Allowed || r.Code != "SECOND_FACTOR_REQUIRED") throw new InvalidOperationException("Independent second factor must be required.");
    }

    private static void FreshReauthenticationIsRequired()
    {
        var s = EnabledService();
        var r = s.AuthorizeArming(Request(false, Valid(StepUpFactorKind.DeviceBoundAuthenticator), Valid(StepUpFactorKind.IndependentSecondFactor)));
        if (r.Allowed || r.Code != "FRESH_REAUTH_REQUIRED") throw new InvalidOperationException("Fresh reauthentication must be required.");
    }

    private static void OrderRoutingRequiresBrokerAndRiskAlerts()
    {
        var s = EnabledService();
        var set = Settings(LiveCommunicationCapability.OrderRouting);
        var r = s.AuthorizeArming(Request(settings: set));
        if (r.Allowed || r.Code != "BROKER_DEPENDENCY_REQUIRED") throw new InvalidOperationException("Order routing must require broker connectivity.");
        set = Settings(LiveCommunicationCapability.OrderRouting, LiveCommunicationCapability.BrokerConnection);
        r = s.AuthorizeArming(Request(settings: set));
        if (r.Allowed || r.Code != "RISK_ALERT_DEPENDENCY_REQUIRED") throw new InvalidOperationException("Order routing must require risk alerts.");
    }

    private static void PositionUpdatesRequireAccountState()
    {
        var s = EnabledService();
        var r = s.AuthorizeArming(Request(settings: Settings(LiveCommunicationCapability.PositionUpdates)));
        if (r.Allowed || r.Code != "ACCOUNT_DEPENDENCY_REQUIRED") throw new InvalidOperationException("Position updates must require account state.");
    }

    private static void ExpiredSessionIsRejected()
    {
        var session = new LiveTradingSession("x", DateTimeOffset.UtcNow.AddMinutes(-20), DateTimeOffset.UtcNow.AddMinutes(-1), true, "s", "a");
        try { LiveTradingSecurityService.RequireArmedSession(session, DateTimeOffset.UtcNow); throw new InvalidOperationException("Expired session was accepted."); }
        catch (InvalidOperationException ex) when (ex.Message == "LIVE_SESSION_NOT_ARMED_OR_EXPIRED") { }
    }

    private static void CommunicationIsExplicitlyScoped()
    {
        var session = new LiveTradingSession("x", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), true, "s", "a");
        var settings = Settings(LiveCommunicationCapability.BrokerConnection);
        try { LiveTradingSecurityService.RequireCommunication(session, settings, LiveCommunicationCapability.OrderRouting, DateTimeOffset.UtcNow); throw new InvalidOperationException("Disabled communication was accepted."); }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("LIVE_COMMUNICATION_DISABLED:")) { }
    }

    private static void BotCannotArmWithoutLiveSession()
    {
        var gate = new LiveBotArmGate();
        var m = Manifest(LiveBotCapability.SendOrders);
        var r = gate.Evaluate(m, false, true, true);
        if (r.Allowed) throw new InvalidOperationException("Bot armed without live session.");
    }

    private static void BotRequiresExplicitOrderCapability()
    {
        var gate = new LiveBotArmGate();
        var m = Manifest();
        var r = gate.Evaluate(m, true, true, true);
        if (r.Allowed || r.Code != "ORDER_CAPABILITY_NOT_GRANTED") throw new InvalidOperationException("Order capability was not explicitly required.");
    }

    private static void BotRetainsEmergencyControl()
    {
        var gate = new LiveBotArmGate();
        var m = new LiveBotManifest("b", new HashSet<LiveBotCapability> { LiveBotCapability.SendOrders }, new HashSet<LiveBotCapability>(), "fp");
        var r = gate.Evaluate(m, true, true, true);
        if (r.Allowed || r.Code != "EMERGENCY_CONTROL_REQUIRED") throw new InvalidOperationException("Emergency control was not retained.");
    }


    private static void PathwayControllerRequiresPrivilegedSettingChange()
    {
        var audit = new InMemoryLiveTradingAuditSink();
        var c = new LiveTradingPathwayController(EnabledService(), audit);
        var invalid = new LiveSecuritySettingChangeRequest("user", DateTimeOffset.UtcNow, true, null, Valid(StepUpFactorKind.IndependentSecondFactor));
        try { c.SetPathwayEnabled(true, invalid, "test"); throw new InvalidOperationException("Incomplete step-up was accepted."); }
        catch (InvalidOperationException ex) when (ex.Message == "DEVICE_FACTOR_REQUIRED") { }
    }

    private static void ArmedStateHasEmergencyDisarm()
    {
        var audit = new InMemoryLiveTradingAuditSink();
        var c = new LiveTradingPathwayController(EnabledService(), audit);
        var auth = new LiveSecuritySettingChangeRequest("user", DateTimeOffset.UtcNow, true, Valid(StepUpFactorKind.DeviceBoundAuthenticator), Valid(StepUpFactorKind.IndependentSecondFactor));
        c.SetPathwayEnabled(true, auth, "test");
        var r = c.Arm(Request());
        if (!r.Allowed || !c.State.EmergencyDisarmAvailable) throw new InvalidOperationException("Armed state must retain emergency disarm.");
        c.EmergencyDisarm("user", "test");
        if (c.State.Armed) throw new InvalidOperationException("Emergency disarm did not clear armed state.");
    }

    private static void AuthorityPolicyRemainsFinalGate()
    {
        var s = EnabledService(new BlockingPolicy());
        var r = s.AuthorizeArming(Request());
        if (r.Allowed || r.Code != "LIVE_AUTHORITY_POLICY_BLOCKED") throw new InvalidOperationException("Authority policy did not remain final gate.");
    }

    private static LiveTradingSecurityService EnabledService(IAuthorityPolicy? p = null) => new(new LiveTradingSecurityPolicy(true, true, true, true, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(5), true, true, true, true), p ?? new AllowingPolicy());
    private static LiveTradingAuthorizationRequest Request(bool fresh = true, StepUpFactorProof? device = null, StepUpFactorProof? second = null, LiveCommunicationSettings? settings = null) => new("user", DateTimeOffset.UtcNow, fresh, device ?? Valid(StepUpFactorKind.DeviceBoundAuthenticator), second ?? Valid(StepUpFactorKind.IndependentSecondFactor), settings ?? Settings(LiveCommunicationCapability.BrokerConnection, LiveCommunicationCapability.RiskAlerts), "strategy", "risk", "runtime");
    private static StepUpFactorProof Valid(StepUpFactorKind k) => new(k, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2));
    private static LiveCommunicationSettings Settings(params LiveCommunicationCapability[] enabled) => new(enabled.ToHashSet(), Enum.GetValues<LiveCommunicationCapability>().Except(enabled).ToHashSet(), Guid.NewGuid().ToString("N"));
    private static LiveBotManifest Manifest(params LiveBotCapability[] enabled) => new("b", enabled.ToHashSet(), new HashSet<LiveBotCapability> { LiveBotCapability.EmergencyFlatten }, "fp");
    private sealed class AllowingPolicy : IAuthorityPolicy { public GovernanceDecision Evaluate(string operation) => new(true, "EXPLICIT_LIVE_POLICY", "explicitly enabled for architecture validation"); }
    private sealed class BlockingPolicy : IAuthorityPolicy { public GovernanceDecision Evaluate(string operation) => new(false, "BLOCK", "blocked"); }
}
