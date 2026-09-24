namespace QuantForge.Governance;

public sealed record LiveTradingPathwayState(
    bool Enabled,
    bool Armed,
    bool EmergencyDisarmAvailable,
    string VisualState,
    LiveCommunicationSettings CommunicationSettings,
    string? SessionId,
    DateTimeOffset? SessionExpiresAt);

/// <summary>
/// Administrative control surface. It deliberately requires the security service for arming and never stores credentials or factor secrets.
/// </summary>
public sealed class LiveTradingPathwayController
{
    private readonly LiveTradingSecurityService _security;
    private readonly ILiveTradingAuditSink _audit;
    private LiveTradingSecurityPolicy _policy;
    private LiveTradingCommunicationController _communications;
    private LiveTradingSession? _session;

    public LiveTradingPathwayController(LiveTradingSecurityService security, ILiveTradingAuditSink audit, LiveTradingSecurityPolicy? policy = null)
    {
        _security = security;
        _audit = audit;
        _policy = policy ?? LiveTradingSecurityPolicy.Default;
        _communications = new LiveTradingCommunicationController(LiveCommunicationSettings.ResearchOnly());
    }

    public LiveTradingPathwayState State => new(_policy.PathwayEnabled, _session is { IsArmed: true }, true,
        _session is { IsArmed: true } ? "LIVE_TRADING_ARMED" : _policy.PathwayEnabled ? "LIVE_PATHWAY_ENABLED_NOT_ARMED" : "RESEARCH_ONLY",
        _communications.Settings, _session?.SessionId, _session?.ExpiresAt);

    public void SetPathwayEnabled(bool enabled, LiveSecuritySettingChangeRequest authorization, string reason)
    {
        var auth = _security.AuthorizeSecuritySettingChange(authorization);
        if (!auth.Allowed) throw new InvalidOperationException(auth.Code);
        var userId = authorization.UserId;
        if (!enabled && _session is { IsArmed: true }) EmergencyDisarm(userId, "Pathway disabled while armed.");
        _policy = _policy with { PathwayEnabled = enabled };
        Append(enabled ? LiveTradingSecurityEvent.PathwayEnabled : LiveTradingSecurityEvent.PathwayDisabled, userId, reason, null);
    }

    public LiveTradingAuthorizationResult Arm(LiveTradingAuthorizationRequest request)
    {
        var result = _security.AuthorizeArming(request);
        Append(result.Allowed ? LiveTradingSecurityEvent.Armed : LiveTradingSecurityEvent.StepUpFailed, request.UserId, result.Reason, result.Session?.SessionId);
        if (result.Allowed) _session = result.Session;
        return result;
    }

    public void EmergencyDisarm(string userId, string reason)
    {
        _session = null;
        Append(LiveTradingSecurityEvent.EmergencyDisarm, userId, reason, null);
    }

    public void SetCommunication(LiveCommunicationCapability capability, bool enabled, LiveSecuritySettingChangeRequest authorization)
    {
        var auth = _security.AuthorizeSecuritySettingChange(authorization);
        if (!auth.Allowed) throw new InvalidOperationException(auth.Code);
        var userId = authorization.UserId;
        _communications = _communications.With(capability, enabled);
        Append(LiveTradingSecurityEvent.SettingsChanged, userId, $"{capability}={(enabled ? "enabled" : "disabled")}", _session?.SessionId);
        if (_session is { IsArmed: true } && !IsCurrentCommunicationSetStillSafe()) EmergencyDisarm(userId, "Communication dependency changed while armed.");
    }

    private bool IsCurrentCommunicationSetStillSafe()
    {
        var e = _communications.Settings.Enabled;
        return !e.Contains(LiveCommunicationCapability.OrderRouting) ||
               (e.Contains(LiveCommunicationCapability.BrokerConnection) && e.Contains(LiveCommunicationCapability.RiskAlerts));
    }

    private void Append(LiveTradingSecurityEvent ev, string userId, string reason, string? sessionId) => _audit.Append(new LiveTradingAuditRecord(Guid.NewGuid().ToString("N"), ev, DateTimeOffset.UtcNow, userId, reason, _communications.Settings.SettingsFingerprint, sessionId, null));
}

public sealed class LiveTradingCommunicationController
{
    public LiveTradingCommunicationController(LiveCommunicationSettings settings) => Settings = settings;
    public LiveCommunicationSettings Settings { get; }

    public LiveTradingCommunicationController With(LiveCommunicationCapability capability, bool enabled)
    {
        var e = Settings.Enabled.ToHashSet();
        var d = Settings.Denied.ToHashSet();
        if (enabled) { e.Add(capability); d.Remove(capability); }
        else { e.Remove(capability); d.Add(capability); }
        if (e.Contains(LiveCommunicationCapability.OrderRouting) && !e.Contains(LiveCommunicationCapability.BrokerConnection)) throw new InvalidOperationException("BROKER_DEPENDENCY_REQUIRED");
        if (e.Contains(LiveCommunicationCapability.OrderRouting) && !e.Contains(LiveCommunicationCapability.RiskAlerts)) throw new InvalidOperationException("RISK_ALERT_DEPENDENCY_REQUIRED");
        if (e.Contains(LiveCommunicationCapability.PositionUpdates) && !e.Contains(LiveCommunicationCapability.AccountState)) throw new InvalidOperationException("ACCOUNT_DEPENDENCY_REQUIRED");
        var fp = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"E:{string.Join(',', e.Order())}|D:{string.Join(',', d.Order())}"))).ToLowerInvariant();
        return new(new LiveCommunicationSettings(e, d, fp));
    }
}
