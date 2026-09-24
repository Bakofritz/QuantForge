using QuantForge.Core;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class MultiCaseExecutionArchitectureChecks
{
    public static async Task RunAsync(IJobLeaseStore leases, IResearchJobStore? jobs = null)
    {
        var identity = new ResearchContextIdentity("", "job-multi-a", "data-a", "hash-a", "strategy-a", "strategy-hash-a", "MES", "1m", "config-a");
        identity = identity with { ContextId = ResearchContextFingerprint.Create(identity) };
        var descriptor = new ResearchContextDescriptor(identity, DateTimeOffset.UnixEpoch, "qf-native-v20.27");
        var identity2 = identity with { JobId = "job-multi-b", StrategyId = "strategy-b", ConfigurationFingerprint = "config-b" };
        identity2 = identity2 with { ContextId = ResearchContextFingerprint.Create(identity2) };
        var descriptor2 = descriptor with { Identity = identity2 };
        var coordinator = new MultiCaseExecutionCoordinator(leases, jobs);
        var calls = 0;
        var result = await coordinator.ExecuteAsync(
            new MultiCaseExecutionRequest("batch-a", "device-a", TimeSpan.FromMinutes(5), 2, new[] { descriptor, descriptor2 }),
            async (context, ct) => { calls++; await Task.Yield(); return ResearchFingerprint.Sha256(context.Descriptor.Identity.ContextId); });
        if (calls != 2 || result.Items.Count != 2 || result.Items.Any(x => x.State != MultiCaseItemState.Completed))
            throw new InvalidOperationException("Multi-case coordinator did not execute isolated cases independently.");
        if (result.AggregateFingerprint.Length != 64) throw new InvalidOperationException("Aggregate fingerprint is not SHA-256.");
    }
}
