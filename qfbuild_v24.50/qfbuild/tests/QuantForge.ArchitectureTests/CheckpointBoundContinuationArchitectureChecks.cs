using System.Text;
using QuantForge.Core;
using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class CheckpointBoundContinuationArchitectureChecks
{
    public static void Run()
    {
        var source = File.ReadAllText(Path.Combine("src", "QuantForge.Runtime", "CheckpointBoundContinuationService.cs"), Encoding.UTF8);
        if (!source.Contains("RECOVERY_CURSOR_REGRESSION", StringComparison.Ordinal)) throw new InvalidOperationException("Cursor regression gate missing.");
        if (!source.Contains("RECOVERY_STEP_STATE_HASH_REQUIRED", StringComparison.Ordinal)) throw new InvalidOperationException("State hash gate missing.");
        if (!source.Contains("SaveCheckpointAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Checkpoint persistence missing.");
        if (!source.Contains("ResearchJobLifecycle.Complete", StringComparison.Ordinal)) throw new InvalidOperationException("Terminal completion transition missing.");
        if (!source.Contains("_leases.RenewAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Lease renewal missing.");
        if (!source.Contains("_leases.ReleaseAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Lease release missing.");
        if (!source.Contains("RECOVERY_CONTINUATION_COMPLETED", StringComparison.Ordinal)) throw new InvalidOperationException("Completion evidence event missing.");
        var fingerprint = ResearchFingerprint.Sha256("checkpoint-bound-continuation");
        if (fingerprint.Length != 64) throw new InvalidOperationException("Fingerprint primitive failed.");
    }
}
