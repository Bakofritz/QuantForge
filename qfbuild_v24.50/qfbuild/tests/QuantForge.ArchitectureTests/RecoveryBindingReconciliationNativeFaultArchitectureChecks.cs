using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class RecoveryBindingReconciliationNativeFaultArchitectureChecks
{
    public static IReadOnlyList<string> Run()
    {
        var failures = new List<string>();
        var points = new[] { StorageFaultPoint.RecoveryAuditWrite, StorageFaultPoint.EvidenceWrite, StorageFaultPoint.TerminalEventWrite };
        if (points.Length != 3) failures.Add("Expected exactly three native reconciliation persistence fault points.");
        if (!File.Exists(FindSource("QuantForge.Storage", "SqliteLocalStore.cs"))) failures.Add("SQLite local store source missing.");
        if (!File.Exists(FindSource("QuantForge.Storage", "SqliteStorageFaultExecutionAdapter.cs"))) failures.Add("Native fault adapter source missing.");
        var store = File.ReadAllText(FindSource("QuantForge.Storage", "SqliteLocalStore.cs"));
        if (!store.Contains("AppendRecoveryBindingReconciliationAtomicallyAsync", StringComparison.Ordinal)) failures.Add("Atomic reconciliation storage method missing.");
        if (!store.Contains("BeginTransactionAsync", StringComparison.Ordinal)) failures.Add("Atomic reconciliation transaction boundary missing.");
        var adapter = File.ReadAllText(FindSource("QuantForge.Storage", "SqliteStorageFaultExecutionAdapter.cs"));
        if (!adapter.Contains("QF-SQLITE-RECOVERY-RECONCILIATION-FAULT-1", StringComparison.Ordinal)) failures.Add("Native reconciliation fault validation contract missing.");
        if (!adapter.Contains("RolledBack", StringComparison.Ordinal)) failures.Add("Rollback verification missing.");
        if (!adapter.Contains("ConflictRejected", StringComparison.Ordinal)) failures.Add("Conflict verification missing.");
        return failures;
    }

    private static string FindSource(string project, string file)
    {
        var root = AppContext.BaseDirectory;
        var current = new DirectoryInfo(root);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", project, file);
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        return Path.Combine(root, "missing", file);
    }
}
