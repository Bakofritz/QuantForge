using QuantForge.Core;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class RecoveryStateIntegrityArchitectureChecks
{
    public static void Validate()
    {
        var expected = Enum.GetNames<RecoveryIntegrityIssueCode>();
        if (expected.Length != 8) throw new InvalidOperationException("Recovery integrity issue coverage is incomplete.");
        if (!typeof(RecoveryStateIntegrityService).GetMethods().Any(m => m.Name == nameof(RecoveryStateIntegrityService.ValidateAsync)))
            throw new InvalidOperationException("Recovery integrity validator is missing.");
        if (!typeof(ResearchRuntime).GetMethods().Any(m => m.Name == nameof(ResearchRuntime.ValidateRecoveryStateIntegrityAsync)))
            throw new InvalidOperationException("ResearchRuntime recovery integrity entry point is missing.");
        if (!typeof(ILeaseInspectionStore).GetMethods().Any(m => m.Name == nameof(ILeaseInspectionStore.LoadLeaseAsync)))
            throw new InvalidOperationException("Lease inspection boundary is missing.");
    }
}
