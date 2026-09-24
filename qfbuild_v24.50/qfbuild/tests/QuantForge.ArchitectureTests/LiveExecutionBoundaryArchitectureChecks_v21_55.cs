using QuantForge.Bots;
using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveExecutionBoundaryArchitectureChecksV21_55
{
    public static void Run()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "QuantForge.Governance", "LiveExecutionBoundaryV21_55.cs"));
        if (!source.Contains("IDEMPOTENCY_KEY_ALREADY_EXISTS", StringComparison.Ordinal)) throw new Exception("Idempotency store guard missing.");
        if (!source.Contains("BROKER_OUTCOME_UNKNOWN", StringComparison.Ordinal)) throw new Exception("Ambiguous outcome guard missing.");
        if (!source.Contains("automatic retry is forbidden", StringComparison.OrdinalIgnoreCase)) throw new Exception("Automatic retry prohibition missing.");
        if (!source.Contains("QueryByIdempotencyKey", StringComparison.Ordinal)) throw new Exception("Reconciliation query boundary missing.");
        if (!source.Contains("_authority.AuthorizeBrokerHandoff", StringComparison.Ordinal)) throw new Exception("Final authority boundary bypass detected.");
        if (!source.Contains("BuildIdempotencyKey", StringComparison.Ordinal)) throw new Exception("Deterministic idempotency binding missing.");
    }
}
