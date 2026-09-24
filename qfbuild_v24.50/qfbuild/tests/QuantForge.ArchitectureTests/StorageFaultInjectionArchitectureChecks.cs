using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class StorageFaultInjectionArchitectureChecks
{
    public static void Validate()
    {
        var contracts = File.ReadAllText(Path.Combine("src", "QuantForge.Storage", "StorageFaultInjectionContracts.cs"));
        var harness = File.ReadAllText(Path.Combine("src", "QuantForge.Storage", "StorageFaultInjectionHarness.cs"));
        var runtime = File.ReadAllText(Path.Combine("src", "QuantForge.Runtime", "ResearchRuntime.cs"));
        foreach (var point in Enum.GetNames<StorageFaultPoint>().Where(x => x != nameof(StorageFaultPoint.None)))
            if (!contracts.Contains(point, StringComparison.Ordinal)) throw new InvalidOperationException($"Fault point missing: {point}");
        if (!harness.Contains("StorageFaultInjectedException", StringComparison.Ordinal)) throw new InvalidOperationException("Injected fault exception path missing.");
        if (!runtime.Contains("ValidateStorageFaultBoundaryAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Runtime fault validation entry point missing.");
        if (typeof(StorageFaultValidationService).GetMethod(nameof(StorageFaultValidationService.ValidateAsync)) is null) throw new InvalidOperationException("Fault validation service missing.");
    }
}
