using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class RecoveryContinuationReconciliationArchitectureChecks
{
    public static RecoveryContinuationReconciliationValidationResult Run() => RecoveryContinuationReconciliationHarness.Run();

    public static string[] RequiredContracts() => new[]
    {
        nameof(IRecoveryContinuationReconciliationStore),
        nameof(RecoveryContinuationReconciliationRecord),
        nameof(RecoveryContinuationReconciliationPolicy),
        nameof(RecoveryContinuationReconciliationHarness)
    };
}
