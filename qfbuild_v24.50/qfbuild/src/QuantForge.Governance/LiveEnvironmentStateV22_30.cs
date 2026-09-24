namespace QuantForge.Governance;

public enum LiveEnvironmentStateV22_30
{
    PlatformNotReady,
    PlatformReady,
    BrokerDisconnected,
    BrokerAuthenticated,
    RecoveryRequired,
    RiskBlocked,
    LivePathwayDisarmed,
    LivePathwayArmed,
    OrderRoutingAvailable,
    OrderRoutingBlocked
}

public sealed record LiveEnvironmentCapabilitySnapshotV22_30(
    LiveEnvironmentStateV22_30 State,
    bool ReadLiveMarketData,
    bool ReadAccountState,
    bool ReadPositions,
    bool ReceiveExecutionReports,
    bool OrderRouting,
    bool CancelOrders,
    bool EmergencyFlatten,
    bool LivePathwayArmed,
    bool RecoveryCleared,
    bool AuditHealthy,
    bool RiskHealthy,
    bool BrokerAuthenticated,
    string Reason);

public static class LiveEnvironmentStateEvaluatorV22_30
{
    public static LiveEnvironmentCapabilitySnapshotV22_30 Evaluate(
        bool platformReady,
        bool brokerAuthenticated,
        bool recoveryCleared,
        bool auditHealthy,
        bool riskHealthy,
        bool livePathwayArmed,
        bool orderRoutingCapability,
        bool accountStateCapability,
        bool positionCapability,
        bool executionReportCapability)
    {
        var observation = brokerAuthenticated;
        var routing = platformReady && brokerAuthenticated && recoveryCleared && auditHealthy &&
                      riskHealthy && livePathwayArmed && orderRoutingCapability;

        var state = !platformReady ? LiveEnvironmentStateV22_30.PlatformNotReady :
            !brokerAuthenticated ? LiveEnvironmentStateV22_30.BrokerDisconnected :
            !recoveryCleared ? LiveEnvironmentStateV22_30.RecoveryRequired :
            !riskHealthy ? LiveEnvironmentStateV22_30.RiskBlocked :
            !livePathwayArmed ? LiveEnvironmentStateV22_30.LivePathwayDisarmed :
            routing ? LiveEnvironmentStateV22_30.OrderRoutingAvailable :
            LiveEnvironmentStateV22_30.OrderRoutingBlocked;

        return new(state, observation, observation && accountStateCapability,
            observation && positionCapability, observation && executionReportCapability,
            routing, routing && orderRoutingCapability, routing && true,
            livePathwayArmed, recoveryCleared, auditHealthy, riskHealthy,
            brokerAuthenticated, routing ? "LIVE_ORDER_ROUTING_GATES_CLEARED" : "LIVE_ORDER_ROUTING_BLOCKED");
    }
}
