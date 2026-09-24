using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed class TerminalCommitIdempotencyValidationService
{
    private readonly SqliteTerminalCommitIdempotencyAdapter _adapter = new();

    public async Task<TerminalCommitIdempotencyValidationResult> ValidateAsync(string databasePath, ILocalEvidenceStore? evidence = null, CancellationToken cancellationToken = default)
    {
        var result = await _adapter.RunAsync(databasePath, cancellationToken);
        if (evidence is not null)
        {
            await evidence.AppendEvidenceAsync($"terminal-idempotency:{result.ResultFingerprint}", result.ResultFingerprint, cancellationToken);
            await evidence.AppendEventAsync($"event:terminal-idempotency:{result.ResultFingerprint}", "terminal-idempotency", "TERMINAL_COMMIT_IDEMPOTENCY_VALIDATION", result.ResultFingerprint, cancellationToken);
        }
        return result;
    }
}
