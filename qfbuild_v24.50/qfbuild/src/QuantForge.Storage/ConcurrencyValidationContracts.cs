namespace QuantForge.Storage;

public sealed record StorageConcurrencyValidationCheck(string Name, bool Passed, string Detail);

public sealed record StorageConcurrencyValidationResult(
    string AdapterVersion,
    bool Passed,
    IReadOnlyList<StorageConcurrencyValidationCheck> Checks,
    string ResultFingerprint,
    bool UsesRealSqliteStorage);

/// <summary>
/// Adapter boundary for exercising concurrency invariants against the actual local storage implementation.
/// Implementations must not enable application-level parallel research execution.
/// </summary>
public interface IConcurrencyStorageValidationAdapter
{
    Task<StorageConcurrencyValidationResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Durable ingestion boundary for storage/concurrency validation results.
/// </summary>
public interface IConcurrencyValidationResultStore
{
    Task AppendConcurrencyValidationResultAsync(StorageConcurrencyValidationResult result, CancellationToken cancellationToken = default);
    Task<StorageConcurrencyValidationResult?> LoadConcurrencyValidationResultAsync(string resultFingerprint, CancellationToken cancellationToken = default);
}
