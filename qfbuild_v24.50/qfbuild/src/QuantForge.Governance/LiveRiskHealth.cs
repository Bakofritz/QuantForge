namespace QuantForge.Governance;

public sealed record LiveRiskHealthSnapshot(bool Healthy, DateTimeOffset ObservedAt, string RiskPolicyFingerprint, string Reason, DateTimeOffset? LastHeartbeat);

public interface ILiveRiskHealthMonitor
{
    LiveRiskHealthSnapshot Snapshot(DateTimeOffset now);
}

public sealed class DeterministicLiveRiskHealthMonitor : ILiveRiskHealthMonitor
{
    private readonly string _riskPolicyFingerprint;
    private readonly TimeSpan _heartbeatTimeout;
    private DateTimeOffset _lastHeartbeat;
    private bool _healthy;

    public DeterministicLiveRiskHealthMonitor(string riskPolicyFingerprint, TimeSpan? heartbeatTimeout = null, DateTimeOffset? initialHeartbeat = null, bool healthy = true)
    {
        _riskPolicyFingerprint = riskPolicyFingerprint; _heartbeatTimeout = heartbeatTimeout ?? TimeSpan.FromSeconds(10); _lastHeartbeat = initialHeartbeat ?? DateTimeOffset.UtcNow; _healthy = healthy;
    }

    public void RecordHeartbeat(DateTimeOffset observedAt) { _lastHeartbeat = observedAt; }
    public void SetHealth(bool healthy) { _healthy = healthy; }
    public LiveRiskHealthSnapshot Snapshot(DateTimeOffset now) => new(_healthy && now - _lastHeartbeat <= _heartbeatTimeout, now, _riskPolicyFingerprint, _healthy ? (now - _lastHeartbeat <= _heartbeatTimeout ? "RISK_HEALTHY" : "RISK_HEARTBEAT_STALE") : "RISK_HEALTH_UNHEALTHY", _lastHeartbeat);
}
