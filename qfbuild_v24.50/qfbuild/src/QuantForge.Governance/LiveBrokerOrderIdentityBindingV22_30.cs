using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public sealed record BrokerOrderIdentityBindingV22_30(
    string ExecutionId,
    string IdempotencyKey,
    string RequestFingerprint,
    string ExternalBrokerOrderId,
    string BrokerSessionId);

public static class BrokerOrderIdentityBindingV22_30Policy
{
    public static bool IsComplete(BrokerOrderIdentityBindingV22_30 binding) =>
        !string.IsNullOrWhiteSpace(binding.ExecutionId) &&
        !string.IsNullOrWhiteSpace(binding.IdempotencyKey) &&
        !string.IsNullOrWhiteSpace(binding.RequestFingerprint) &&
        !string.IsNullOrWhiteSpace(binding.ExternalBrokerOrderId) &&
        !string.IsNullOrWhiteSpace(binding.BrokerSessionId);

    public static string ComputeFingerprint(BrokerOrderIdentityBindingV22_30 binding)
    {
        var canonical = string.Join("|", binding.ExecutionId, binding.IdempotencyKey,
            binding.RequestFingerprint, binding.ExternalBrokerOrderId, binding.BrokerSessionId);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
