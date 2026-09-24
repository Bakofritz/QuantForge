namespace QuantForge.Governance;

public enum NormalizedLiveOrderStateV22_15 { Unknown, Pending, Accepted, PartiallyFilled, Filled, Canceled, Rejected }
public sealed record BrokerOrderStateObservationV22_15(string ExternalOrderId, string BrokerState, bool Definitive, string IdempotencyKey, string RequestFingerprint);
public sealed record NormalizedLiveOrderStateResultV22_15(NormalizedLiveOrderStateV22_15 State, bool Definitive, string Code);

public static class LiveBrokerOrderStateNormalizerV22_15
{
    public static NormalizedLiveOrderStateResultV22_15 Normalize(BrokerOrderStateObservationV22_15 observation)
    {
        if (string.IsNullOrWhiteSpace(observation.IdempotencyKey) || string.IsNullOrWhiteSpace(observation.RequestFingerprint))
            return new(NormalizedLiveOrderStateV22_15.Unknown, false, "BROKER_IDENTITY_BINDING_MISSING");
        var s = observation.BrokerState.Trim().ToUpperInvariant();
        var state = s switch
        {
            "PENDING" or "NEW" => NormalizedLiveOrderStateV22_15.Pending,
            "ACCEPTED" or "OPEN" => NormalizedLiveOrderStateV22_15.Accepted,
            "PARTIAL" or "PARTIALLY_FILLED" => NormalizedLiveOrderStateV22_15.PartiallyFilled,
            "FILLED" or "EXECUTED" => NormalizedLiveOrderStateV22_15.Filled,
            "CANCELED" or "CANCELLED" => NormalizedLiveOrderStateV22_15.Canceled,
            "REJECTED" or "DECLINED" => NormalizedLiveOrderStateV22_15.Rejected,
            _ => NormalizedLiveOrderStateV22_15.Unknown
        };
        return new(state, observation.Definitive && state != NormalizedLiveOrderStateV22_15.Unknown,
            state == NormalizedLiveOrderStateV22_15.Unknown ? "BROKER_STATE_UNMAPPED" : "BROKER_STATE_NORMALIZED");
    }
}
