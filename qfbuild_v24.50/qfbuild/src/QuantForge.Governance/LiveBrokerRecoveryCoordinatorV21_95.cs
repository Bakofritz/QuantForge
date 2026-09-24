namespace QuantForge.Governance;

public sealed record LiveExecutionRecoveryStateV21_95(bool Blocked, string Code, string Reason, int UnknownIntentCount, LiveBrokerConnectionState TransportState);

/// <summary>Coordinates transport recovery without converting reconnect into order permission.</summary>
public sealed class LiveBrokerRecoveryCoordinatorV21_95
{
    private readonly BrokerTransportReconnectPolicyV21_85 _reconnectPolicy;
    private int _attempts;
    public LiveBrokerRecoveryCoordinatorV21_95(BrokerTransportReconnectPolicyV21_85? reconnectPolicy = null) => _reconnectPolicy = reconnectPolicy ?? new BrokerTransportReconnectPolicyV21_85();

    public bool CanReconnect(LiveBrokerTransportHealth health) => _reconnectPolicy.ShouldReconnect(health, _attempts);
    public void RecordReconnectAttempt() => _attempts++;
    public void ResetReconnectAttempts() => _attempts = 0;

    public LiveExecutionRecoveryStateV21_95 Evaluate(LiveBrokerTransportHealth health, int unknownIntentCount)
    {
        if (unknownIntentCount > 0)
            return new(true, "UNKNOWN_EXECUTION_REQUIRES_RECONCILIATION", "One or more execution intents have an ambiguous broker outcome.", unknownIntentCount, health.State);
        if (health.State != LiveBrokerConnectionState.Connected)
            return new(true, "BROKER_TRANSPORT_NOT_READY", "Broker transport is not connected; live execution must remain blocked.", 0, health.State);
        return new(false, "READY_FOR_AUTHORITY_RECHECK", "Transport and execution-intent recovery state are clear; final authority checks are still required.", 0, health.State);
    }
}
