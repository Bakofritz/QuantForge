using QuantForge.Core;
using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class ResearchIsolationArchitectureChecks
{
    public static void Run()
    {
        var identity = new ResearchContextIdentity("", "job-1", "data-1", "datahash", "strat-a", "strathash", "MES", "1m", "conf-a");
        var contextId = ResearchContextFingerprint.Create(identity);
        identity = identity with { ContextId = contextId };
        var descriptor = new ResearchContextDescriptor(identity, DateTimeOffset.UnixEpoch, "qf-native-v20.26");
        var coordinator = new IsolatedResearchCoordinator();
        var a = coordinator.Create(descriptor);
        var b = coordinator.Create(descriptor with { Identity = identity with { ContextId = ResearchContextFingerprint.Create(identity with { StrategyId = "strat-b" }), StrategyId = "strat-b" } });
        a.SetPosition(1); a.SetStrategyValue("fast", 9); a.AddEvidenceReference("evidence-a");
        if (b.Position != 0 || b.StrategyValues.Count != 0 || b.EvidenceReferences.Count != 0) throw new InvalidOperationException("Mutable research state leaked between contexts.");
        if (a.Descriptor.Identity.ContextId == b.Descriptor.Identity.ContextId) throw new InvalidOperationException("Distinct research cases share a context identity.");
        var c = coordinator.Create(new IsolatedResearchCaseRequest("job-2", "data-2", "datahash-2", "strat-c", "strathash-c", "MNQ", "5m", "conf-c"), "qf-native-v20.26");
        if (c.Descriptor.Identity.ContextId.Length != 64) throw new InvalidOperationException("Context fingerprint is not deterministic SHA-256.");
        var batch = coordinator.CreateBatch(new[] { a.Descriptor, b.Descriptor });
        if (batch.Count != 2) throw new InvalidOperationException("Batch isolation failed.");
    }
}
