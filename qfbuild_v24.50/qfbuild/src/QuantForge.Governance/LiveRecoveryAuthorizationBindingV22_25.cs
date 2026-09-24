using System;
using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public sealed record RecoveryAuthorizationBindingV22_25(string ExecutionId, string IdempotencyKey, string RequestFingerprint, string AuthorityFingerprint);

public static class RecoveryAuthorizationBindingV22_25Policy
{
    public static string ComputeAuthorityFingerprint(RecoveryAuthorizationBindingV22_25 binding)
    {
        var canonical = $"{binding.ExecutionId}|{binding.IdempotencyKey}|{binding.RequestFingerprint}|{binding.AuthorityFingerprint}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static bool Matches(RecoveryAuthorizationBindingV22_25 expected, RecoveryAuthorizationBindingV22_25 observed) =>
        expected.ExecutionId == observed.ExecutionId && expected.IdempotencyKey == observed.IdempotencyKey &&
        expected.RequestFingerprint == observed.RequestFingerprint && expected.AuthorityFingerprint == observed.AuthorityFingerprint;
}
