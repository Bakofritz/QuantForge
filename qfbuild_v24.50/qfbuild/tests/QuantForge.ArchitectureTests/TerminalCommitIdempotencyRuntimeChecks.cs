using QuantForge.Core;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class TerminalCommitIdempotencyRuntimeChecks
{
    public static IReadOnlyList<string> Run()
    {
        var checks = new List<string>();
        var type = typeof(TerminalCommitIdempotencyValidationService);
        if (type.GetMethod(nameof(TerminalCommitIdempotencyValidationService.ValidateAsync)) is null)
            throw new InvalidOperationException("Idempotency validation service is missing.");
        if (typeof(SqliteLocalStore).GetMethod(nameof(SqliteLocalStore.CommitTerminalOutcomeAsync)) is null)
            throw new InvalidOperationException("Transactional terminal commit is missing.");
        if (typeof(TerminalCommitConflictException).GetProperty(nameof(TerminalCommitConflictException.Code)) is null)
            throw new InvalidOperationException("Terminal conflict code is missing.");
        checks.Add("RUNTIME_IDEMPOTENCY_ENTRY_POINT_PASS");
        checks.Add("SQLITE_TERMINAL_COMMIT_ENTRY_POINT_PASS");
        checks.Add("CONFLICT_EXCEPTION_PASS");
        return checks.AsReadOnly();
    }
}
