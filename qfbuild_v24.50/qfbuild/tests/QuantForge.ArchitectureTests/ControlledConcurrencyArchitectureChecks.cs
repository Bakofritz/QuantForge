using QuantForge.Core;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class ControlledConcurrencyArchitectureChecks
{
    public static void Run()
    {
        var readiness = typeof(ControlledConcurrencyReadinessResult);
        if (readiness.GetProperty(nameof(ControlledConcurrencyReadinessResult.ParallelExecutionEnabled)) is null)
            throw new InvalidOperationException("Concurrency result must expose an explicit parallel execution gate.");
        var agreement = typeof(TerminalStateAgreementResult);
        if (agreement.GetProperty(nameof(TerminalStateAgreementResult.AgreementFingerprint)) is null)
            throw new InvalidOperationException("Terminal agreement must be fingerprinted.");
        if (typeof(IJobLeaseStore).GetMethod(nameof(IJobLeaseStore.RenewAsync)) is null)
            throw new InvalidOperationException("Lease renewal is required before controlled concurrency.");
        if (typeof(IResourceReceiptStore).GetMethod(nameof(IResourceReceiptStore.LoadExecutionReceiptsForBatchAsync)) is null)
            throw new InvalidOperationException("Batch receipt retrieval is required before controlled concurrency.");
        if (nameof(MultiCaseExecutionCoordinator).Length == 0)
            throw new InvalidOperationException("Coordinator contract missing.");
    }
}
