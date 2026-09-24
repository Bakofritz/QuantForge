using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class StorageFaultRecoveryArchitectureChecks
{
    public static IReadOnlyList<string> Run()
    {
        var checks = new List<string>();
        var points = Enum.GetValues<StorageFaultPoint>().Where(p => p != StorageFaultPoint.None).ToArray();
        if (points.Length != 7) throw new InvalidOperationException("Expected seven storage fault points.");
        checks.Add("FAULT_POINTS=7");
        if (typeof(SqliteStorageFaultExecutionAdapter).GetMethod("RunRecoveryInvariantAsync") is null)
            throw new InvalidOperationException("Native recovery-invariant adapter entry point missing.");
        checks.Add("NATIVE_RECOVERY_ADAPTER_PRESENT");
        if (typeof(StorageFaultRecoveryValidationService).GetMethod("ValidateAsync") is null)
            throw new InvalidOperationException("Storage fault recovery validation service missing.");
        checks.Add("RECOVERY_VALIDATION_SERVICE_PRESENT");
        if (typeof(StorageFaultRecoveryValidationResult).GetProperty("FailClosedExpected") is null)
            throw new InvalidOperationException("Fail-closed recovery contract missing.");
        checks.Add("FAIL_CLOSED_CONTRACT_PRESENT");
        return checks;
    }
}
