using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class TransactionalTerminalFaultArchitectureChecks
{
    public static IReadOnlyList<string> Validate(string source)
    {
        var failures = new List<string>();
        foreach (var point in Enum.GetNames<TransactionalTerminalFaultPoint>())
            if (!source.Contains(point, StringComparison.Ordinal)) failures.Add($"MISSING_TRANSACTIONAL_FAULT_POINT:{point}");
        if (!source.Contains("CommitTerminalOutcomeAsync", StringComparison.Ordinal)) failures.Add("MISSING_TERMINAL_COMMIT");
        if (!source.Contains("TerminalEventWrite", StringComparison.Ordinal)) failures.Add("MISSING_EVENT_FAULT");
        if (!source.Contains("TerminalCommitBeforeCommit", StringComparison.Ordinal)) failures.Add("MISSING_BEFORE_COMMIT_FAULT");
        if (!source.Contains("ValidateTransactionalTerminalFaultAsync", StringComparison.Ordinal)) failures.Add("MISSING_RUNTIME_VALIDATOR");
        return failures;
    }
}
