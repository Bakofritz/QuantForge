using QuantForge.Core;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class ResourceGovernanceArchitectureChecks
{
    public static void Run()
    {
        var policy = MultiCaseResourcePolicy.Conservative(TimeSpan.FromMinutes(3));
        policy.Validate();
        if (policy.MaxConcurrentCases != 1) throw new Exception("Sequential gate missing.");
        var fingerprint = ResearchFingerprint.Sha256("batch|context|job|COMPLETED|result|0|10");
        if (fingerprint.Length != 64) throw new Exception("Receipt fingerprint missing.");
        var receipt = new MultiCaseExecutionReceipt("b","c","j","worker","workerfp",MultiCaseItemState.Completed,"r",fingerprint,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,10,1,"WITHIN_POLICY",null);
        if (receipt.State != MultiCaseItemState.Completed || receipt.HeartbeatRenewals != 1) throw new Exception("Receipt contract invalid.");
        if (!typeof(IJobLeaseStore).GetMethod(nameof(IJobLeaseStore.RenewAsync))!.Name.Equals("RenewAsync", StringComparison.Ordinal)) throw new Exception("Lease renewal contract missing.");
    }
}

// v20.30 checks: durable resource receipt and worker identity contracts.

internal static class ResourceGovernanceV2030Checks
{
    public static void Run()
    {
        var identity = new QuantForge.Core.ResearchWorkerIdentity("worker-a", "device-a", "qf-native-v20.30");
        if (identity.IdentityFingerprint.Length != 64) throw new InvalidOperationException("Worker identity fingerprint must be SHA-256.");
        var receipt = typeof(QuantForge.Storage.ResourceReceiptRecord);
        var batch = typeof(QuantForge.Storage.BatchResourceRecord);
        if (receipt.GetProperty("WorkerFingerprint") is null || receipt.GetProperty("ReceiptFingerprint") is null) throw new InvalidOperationException("Durable receipt contract missing identity fields.");
        if (batch.GetProperty("RecordFingerprint") is null || batch.GetProperty("TotalHeartbeatRenewals") is null) throw new InvalidOperationException("Batch resource accounting contract incomplete.");
    }
}
