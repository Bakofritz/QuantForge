namespace QuantForge.Governance;

public sealed class LiveRiskSafetyCoordinator
{
    private readonly RiskHealthMonitor _monitor;
    private readonly LiveTradingPathwayController _pathway;
    private readonly string _systemUserId;
    public LiveRiskSafetyCoordinator(RiskHealthMonitor monitor, LiveTradingPathwayController pathway, string systemUserId = "SYSTEM")
    { _monitor = monitor; _pathway = pathway; _systemUserId = systemUserId; }

    public RiskHealthSnapshot EvaluateAndProtect(DateTimeOffset now)
    {
        var snapshot = _monitor.Evaluate(now);
        if (snapshot.State is RiskHealthState.Stale or RiskHealthState.Failed)
            _pathway.EmergencyDisarm(_systemUserId, "Risk heartbeat is stale or failed; automatic emergency disarm applied.");
        return snapshot;
    }
}
