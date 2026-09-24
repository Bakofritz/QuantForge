using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public enum StepUpFactorKind
{
    DeviceBoundAuthenticator,
    IndependentSecondFactor
}

public sealed record StepUpFactorProof(StepUpFactorKind Kind, string ProofId, DateTimeOffset VerifiedAt, TimeSpan ValidFor);

public sealed record LiveTradingSession(
    string SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsArmed,
    string SettingsFingerprint,
    string AuthorizationFingerprint);

public enum LiveCommunicationCapability
{
    BrokerConnection,
    LiveMarketData,
    OrderRouting,
    AccountState,
    PositionUpdates,
    ExecutionReports,
    Heartbeat,
    RiskAlerts,
    AuditTelemetry,
    UserNotifications
}

public sealed record LiveCommunicationSettings(
    IReadOnlySet<LiveCommunicationCapability> Enabled,
    IReadOnlySet<LiveCommunicationCapability> Denied,
    string SettingsFingerprint)
{
    public static LiveCommunicationSettings ResearchOnly() =>
        new(new HashSet<LiveCommunicationCapability>(),
            Enum.GetValues<LiveCommunicationCapability>().ToHashSet(),
            Fingerprint(Array.Empty<LiveCommunicationCapability>(), Enum.GetValues<LiveCommunicationCapability>()));

    private static string Fingerprint(IEnumerable<LiveCommunicationCapability> enabled, IEnumerable<LiveCommunicationCapability> denied) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"E:{string.Join(',', enabled.Order())}|D:{string.Join(',', denied.Order())}"))).ToLowerInvariant();
}

public sealed record LiveTradingSecurityPolicy(
    bool PathwayEnabled,
    bool RequireFreshLoginReauthentication,
    bool RequireDeviceBoundFactor,
    bool RequireIndependentSecondFactor,
    TimeSpan MaximumArmedSession,
    TimeSpan MaximumFactorAge,
    bool RequireExplicitArmAction,
    bool RequireExplicitDisarmAction,
    bool RequireAuditTrail,
    bool FailClosedOnCommunicationDependencyLoss)
{
    public static LiveTradingSecurityPolicy Default => new(
        PathwayEnabled: false,
        RequireFreshLoginReauthentication: true,
        RequireDeviceBoundFactor: true,
        RequireIndependentSecondFactor: true,
        MaximumArmedSession: TimeSpan.FromMinutes(15),
        MaximumFactorAge: TimeSpan.FromMinutes(5),
        RequireExplicitArmAction: true,
        RequireExplicitDisarmAction: true,
        RequireAuditTrail: true,
        FailClosedOnCommunicationDependencyLoss: true);
}

public sealed record LiveTradingAuthorizationRequest(
    string UserId,
    DateTimeOffset RequestedAt,
    bool FreshLoginReauthenticated,
    StepUpFactorProof? DeviceFactor,
    StepUpFactorProof? IndependentFactor,
    LiveCommunicationSettings CommunicationSettings,
    string StrategyRegistryFingerprint,
    string RiskPolicyFingerprint,
    string RuntimeQualificationFingerprint);

public sealed record LiveTradingAuthorizationResult(bool Allowed, string Code, string Reason, LiveTradingSession? Session = null);

public sealed record LiveSecuritySettingChangeRequest(
    string UserId,
    DateTimeOffset RequestedAt,
    bool FreshLoginReauthenticated,
    StepUpFactorProof? DeviceFactor,
    StepUpFactorProof? IndependentFactor);


public sealed class LiveTradingSecurityService
{
    private readonly LiveTradingSecurityPolicy _policy;
    private readonly IAuthorityPolicy _authorityPolicy;

    public LiveTradingSecurityService(LiveTradingSecurityPolicy? policy = null, IAuthorityPolicy? authorityPolicy = null)
    {
        _policy = policy ?? LiveTradingSecurityPolicy.Default;
        _authorityPolicy = authorityPolicy ?? new DeterministicAuthorityPolicy();
    }

    public GovernanceDecision AuthorizeSecuritySettingChange(LiveSecuritySettingChangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.FreshLoginReauthenticated && _policy.RequireFreshLoginReauthentication) return new(false, "FRESH_REAUTH_REQUIRED", "A fresh login reauthentication is required.");
        if (!ValidFactor(request.DeviceFactor, StepUpFactorKind.DeviceBoundAuthenticator)) return new(false, "DEVICE_FACTOR_REQUIRED", "A fresh device-bound authenticator proof is required.");
        if (!ValidFactor(request.IndependentFactor, StepUpFactorKind.IndependentSecondFactor)) return new(false, "SECOND_FACTOR_REQUIRED", "A fresh independent second-factor proof is required.");
        return new(true, "SECURITY_SETTING_AUTHORIZED", "Security setting change is authorized for this administrative operation.");
    }

    public LiveTradingAuthorizationResult AuthorizeArming(LiveTradingAuthorizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_policy.PathwayEnabled) return Denied("LIVE_PATHWAY_DISABLED", "The live-trading pathway is disabled by security policy.");
        if (!_policy.RequireExplicitArmAction) return Denied("ARM_POLICY_INVALID", "Live arming requires an explicit arm action.");
        if (!request.FreshLoginReauthenticated && _policy.RequireFreshLoginReauthentication) return Denied("FRESH_REAUTH_REQUIRED", "A fresh login reauthentication is required.");
        if (!ValidFactor(request.DeviceFactor, StepUpFactorKind.DeviceBoundAuthenticator)) return Denied("DEVICE_FACTOR_REQUIRED", "A fresh device-bound authenticator proof is required.");
        if (!ValidFactor(request.IndependentFactor, StepUpFactorKind.IndependentSecondFactor)) return Denied("SECOND_FACTOR_REQUIRED", "A fresh independent second-factor proof is required.");
        if (request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.OrderRouting) &&
            !request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.BrokerConnection))
            return Denied("BROKER_DEPENDENCY_REQUIRED", "Order routing requires an enabled broker connection.");
        if (request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.ExecutionReports) &&
            !request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.BrokerConnection))
            return Denied("BROKER_DEPENDENCY_REQUIRED", "Execution reports require an enabled broker connection.");
        if (request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.PositionUpdates) &&
            !request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.AccountState))
            return Denied("ACCOUNT_DEPENDENCY_REQUIRED", "Position updates require account state communications.");
        if (request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.OrderRouting) &&
            !request.CommunicationSettings.Enabled.Contains(LiveCommunicationCapability.RiskAlerts))
            return Denied("RISK_ALERT_DEPENDENCY_REQUIRED", "Order routing requires risk-alert communications.");
        var authority = _authorityPolicy.Evaluate("LIVE_TRADING");
        if (!authority.Allowed) return Denied("LIVE_AUTHORITY_POLICY_BLOCKED", authority.Reason);
        if (string.IsNullOrWhiteSpace(request.StrategyRegistryFingerprint) || string.IsNullOrWhiteSpace(request.RiskPolicyFingerprint) || string.IsNullOrWhiteSpace(request.RuntimeQualificationFingerprint))
            return Denied("LIVE_BINDINGS_REQUIRED", "Strategy, risk-policy, and runtime qualification bindings are required.");

        var now = request.RequestedAt;
        var expires = now.Add(_policy.MaximumArmedSession);
        var authFp = Fingerprint(request.UserId, request.CommunicationSettings.SettingsFingerprint, request.StrategyRegistryFingerprint, request.RiskPolicyFingerprint, request.RuntimeQualificationFingerprint, now);
        var session = new LiveTradingSession($"live-{Guid.NewGuid():N}", now, expires, true, request.CommunicationSettings.SettingsFingerprint, authFp);
        return new(true, "LIVE_ARMED", "Live trading pathway armed under two-factor step-up authorization.", session);
    }

    public static void RequireArmedSession(LiveTradingSession? session, DateTimeOffset now)
    {
        if (session is null || !session.IsArmed || now >= session.ExpiresAt)
            throw new InvalidOperationException("LIVE_SESSION_NOT_ARMED_OR_EXPIRED");
    }

    public static void RequireCommunication(LiveTradingSession session, LiveCommunicationSettings settings, LiveCommunicationCapability capability, DateTimeOffset now)
    {
        RequireArmedSession(session, now);
        if (!settings.Enabled.Contains(capability)) throw new InvalidOperationException($"LIVE_COMMUNICATION_DISABLED:{capability}");
    }

    private bool ValidFactor(StepUpFactorProof? proof, StepUpFactorKind expected) =>
        proof is not null && proof.Kind == expected && proof.VerifiedAt <= DateTimeOffset.UtcNow && DateTimeOffset.UtcNow - proof.VerifiedAt <= _policy.MaximumFactorAge && proof.ValidFor > TimeSpan.Zero;

    private static LiveTradingAuthorizationResult Denied(string code, string reason) => new(false, code, reason);
    private static string Fingerprint(params object[] values) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', values)))).ToLowerInvariant();
}
