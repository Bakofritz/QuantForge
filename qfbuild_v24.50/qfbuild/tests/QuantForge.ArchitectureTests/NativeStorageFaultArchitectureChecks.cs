using System.Text;

namespace QuantForge.ArchitectureTests;

public static class NativeStorageFaultArchitectureChecks
{
    public static IReadOnlyList<string> Run(string sourceRoot)
    {
        var failures = new List<string>();
        var store = File.ReadAllText(Path.Combine(sourceRoot, "src", "QuantForge.Storage", "SqliteLocalStore.cs"));
        var adapter = File.ReadAllText(Path.Combine(sourceRoot, "src", "QuantForge.Storage", "SqliteStorageFaultExecutionAdapter.cs"));
        var runtime = File.ReadAllText(Path.Combine(sourceRoot, "src", "QuantForge.Runtime", "ResearchRuntime.cs"));
        foreach (var point in new[] { "LeaseAcquire", "LeaseRenew", "CheckpointWrite", "ReceiptWrite", "EvidenceWrite", "ValidationResultWrite", "JobStateWrite" })
            if (!store.Contains($"StorageFaultPoint.{point}", StringComparison.Ordinal)) failures.Add($"MISSING_FAULT_POINT:{point}");
        if (!store.Contains("internal SqliteLocalStore(string databasePath, StorageFaultInjector? faultInjector)", StringComparison.Ordinal)) failures.Add("MISSING_TEST_ONLY_SQLITE_SEAM");
        if (!adapter.Contains("true);", StringComparison.Ordinal)) failures.Add("MISSING_NATIVE_STORAGE_FLAG");
        if (!adapter.Contains("ObservedAndContained", StringComparison.Ordinal)) failures.Add("MISSING_FAIL_CLOSED_OBSERVATION");
        if (!runtime.Contains("ValidateNativeStorageFaultBoundaryAsync", StringComparison.Ordinal)) failures.Add("MISSING_RUNTIME_ENTRY_POINT");
        return failures;
    }
}
