using System;
using System.IO;

namespace QuantForge.ArchitectureTests;

public static class RecoveryPreflightBindingArchitectureChecks
{
    public static void Run()
    {
        var root = Path.Combine("..", "..", "..", "..", "src");
        var contracts = File.ReadAllText(Path.Combine(root, "QuantForge.Storage", "RecoveryPreflightContracts.cs"));
        var service = File.ReadAllText(Path.Combine(root, "QuantForge.Runtime", "RecoveryPreflightService.cs"));
        var continuation = File.ReadAllText(Path.Combine(root, "QuantForge.Runtime", "RecoveryContinuationService.cs"));
        var sqlite = File.ReadAllText(Path.Combine(root, "QuantForge.Storage", "SqliteLocalStore.cs"));
        foreach (var term in new[] { "CheckpointSequence", "CheckpointCursor", "CheckpointStateHash", "AcceptedLeaseVersion", "AcceptedLeaseFingerprint" })
            if (!contracts.Contains(term, StringComparison.Ordinal)) throw new InvalidOperationException($"Recovery preflight binding field missing: {term}");
        if (!service.Contains("acceptedLeaseFingerprint", StringComparison.Ordinal)) throw new InvalidOperationException("Accepted lease fingerprint binding missing.");
        if (!continuation.Contains("acceptedLease", StringComparison.Ordinal) || !continuation.Contains("RECOVERY_ACCEPTED_LEASE_DISAPPEARED", StringComparison.Ordinal)) throw new InvalidOperationException("Recovery continuation lease binding missing.");
        if (!sqlite.Contains("accepted_lease_fingerprint", StringComparison.Ordinal) || !sqlite.Contains("checkpoint_hash", StringComparison.Ordinal)) throw new InvalidOperationException("SQLite recovery preflight binding persistence missing.");
    }
}
