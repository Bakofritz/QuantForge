using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>Records native storage fault/retry invariants without granting any operational authority.</summary>
public sealed class StorageFaultRecoveryValidationService
{
    private readonly SqliteStorageFaultExecutionAdapter _adapter;

    public StorageFaultRecoveryValidationService(SqliteStorageFaultExecutionAdapter? adapter = null)
        => _adapter = adapter ?? new SqliteStorageFaultExecutionAdapter();

    public async Task<StorageFaultRecoveryValidationResult> ValidateAsync(
        string databasePath,
        ILocalEvidenceStore? evidence = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _adapter.RunRecoveryInvariantAsync(databasePath, cancellationToken);
        if (evidence is not null)
        {
            var evidenceId = $"storage-fault-recovery:{result.ResultFingerprint}";
            await evidence.AppendEvidenceAsync(evidenceId, result.ResultFingerprint, cancellationToken);
            await evidence.AppendEventAsync(
                $"event:storage-fault-recovery:{result.ResultFingerprint}",
                "storage-fault-validation",
                "STORAGE_FAULT_RECOVERY_VALIDATION",
                result.ResultFingerprint,
                cancellationToken);
        }
        return result;
    }
}
