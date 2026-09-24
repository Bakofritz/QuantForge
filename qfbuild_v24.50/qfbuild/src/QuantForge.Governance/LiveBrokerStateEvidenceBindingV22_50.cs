namespace QuantForge.Governance;

public sealed record LiveBrokerStateEvidenceBindingV22_50(string ExecutionId,string IdempotencyKey,string RequestFingerprint,string ExternalOrderId,string BrokerSessionId);
public static class LiveBrokerStateEvidenceBindingV22_50Policy
{
 public static bool Matches(LiveBrokerStateEvidenceBindingV22_50 e,string executionId,string idempotencyKey,string requestFingerprint,string brokerSessionId) =>
 !string.IsNullOrWhiteSpace(e.ExternalOrderId)&&e.ExecutionId==executionId&&e.IdempotencyKey==idempotencyKey&&e.RequestFingerprint==requestFingerprint&&e.BrokerSessionId==brokerSessionId;
}
