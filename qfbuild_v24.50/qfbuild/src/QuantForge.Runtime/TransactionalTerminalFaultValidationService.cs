using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>Runs the SQLite transactional terminal-commit fault harness and records its result.</summary>
public sealed class TransactionalTerminalFaultValidationService
{
    private readonly SqliteTransactionalTerminalFaultAdapter _adapter;

    public TransactionalTerminalFaultValidationService(SqliteTransactionalTerminalFaultAdapter? adapter = null)
        => _adapter = adapter ?? new SqliteTransactionalTerminalFaultAdapter();

    public async Task<TransactionalTerminalFaultValidationResult> ValidateAsync(
        string databasePath,
        ILocalEvidenceStore? evidence = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _adapter.RunAsync(databasePath, cancellationToken);
        if (evidence is not null)
        {
            var evidenceId = $"transactional-terminal-fault:{result.ResultFingerprint}";
            await evidence.AppendEvidenceAsync(evidenceId, result.ResultFingerprint, cancellationToken);
            await evidence.AppendEventAsync(
                $"event:transactional-terminal-fault:{result.ResultFingerprint}",
                "transactional-terminal-fault-validation",
                "TRANSACTIONAL_TERMINAL_FAULT_VALIDATION",
                result.ResultFingerprint,
                cancellationToken);
        }
        return result;
    }
}
