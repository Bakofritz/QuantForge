using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class SqliteConcurrencyAdapterArchitectureChecks
{
    public static void Validate()
    {
        var adapter = File.ReadAllText(Path.Combine("src", "QuantForge.Storage", "SqliteConcurrencyValidationAdapter.cs"));
        var contracts = File.ReadAllText(Path.Combine("src", "QuantForge.Storage", "ConcurrencyValidationContracts.cs"));
        var store = File.ReadAllText(Path.Combine("src", "QuantForge.Storage", "SqliteLocalStore.cs"));
        var runtime = File.ReadAllText(Path.Combine("src", "QuantForge.Runtime", "ResearchRuntime.cs"));
        if (!contracts.Contains("IConcurrencyStorageValidationAdapter", StringComparison.Ordinal)) throw new InvalidOperationException("Storage concurrency adapter contract missing.");
        if (!adapter.Contains("SqliteLocalStore", StringComparison.Ordinal) || !adapter.Contains("SQLITE_LEASE_RACE", StringComparison.Ordinal)) throw new InvalidOperationException("SQLite contention adapter is incomplete.");
        if (!store.Contains("IConcurrencyValidationResultStore", StringComparison.Ordinal) || !store.Contains("concurrency_validation_results", StringComparison.Ordinal)) throw new InvalidOperationException("Durable concurrency validation result storage missing.");
        if (!store.Contains("Immutable checkpoint conflict detected", StringComparison.Ordinal)) throw new InvalidOperationException("Checkpoint immutability gate missing.");
        if (!runtime.Contains("ValidateStorageConcurrencyAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Runtime storage validation entry point missing.");
    }
}
