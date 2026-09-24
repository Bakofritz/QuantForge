namespace QuantForge.Governance;

public enum RiskHealthState { Unknown, Healthy, Stale, Failed, EmergencyDisarmed }
public sealed record RiskHealthSnapshot(DateTimeOffset ObservedAt, DateTimeOffset LastHeartbeatAt, TimeSpan MaximumHeartbeatAge, RiskHealthState State, string Fingerprint);

public sealed class RiskHealthMonitor
{
    private readonly TimeSpan _maximumAge;
    private RiskHealthSnapshot _snapshot;
    public RiskHealthMonitor(TimeSpan? maximumAge = null)
    {
        _maximumAge = maximumAge ?? TimeSpan.FromSeconds(10);
        var now = DateTimeOffset.UtcNow;
        _snapshot = new(now, DateTimeOffset.MinValue, _maximumAge, RiskHealthState.Unknown, "initial");
    }
    public RiskHealthSnapshot Snapshot => _snapshot;
    public void RecordHeartbeat(DateTimeOffset observedAt, string fingerprint) =>
        _snapshot = new(observedAt, observedAt, _maximumAge, RiskHealthState.Healthy, fingerprint);
    public RiskHealthSnapshot Evaluate(DateTimeOffset now)
    {
        var state = _snapshot.LastHeartbeatAt == DateTimeOffset.MinValue ? RiskHealthState.Unknown :
            now - _snapshot.LastHeartbeatAt > _maximumAge ? RiskHealthState.Stale : RiskHealthState.Healthy;
        _snapshot = _snapshot with { ObservedAt = now, State = state };
        return _snapshot;
    }
    public bool IsHealthy(DateTimeOffset now) => Evaluate(now).State == RiskHealthState.Healthy;
}
