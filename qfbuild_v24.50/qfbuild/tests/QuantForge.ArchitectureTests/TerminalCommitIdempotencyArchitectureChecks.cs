using QuantForge.Core;

namespace QuantForge.ArchitectureTests;

public static class TerminalCommitIdempotencyArchitectureChecks
{
    public static IReadOnlyList<string> Run()
    {
        var checks = new List<string>();
        if (!TerminalCommitRules.IsTerminal(nameof(DurableJobState.Completed))) throw new InvalidOperationException("Completed must be terminal.");
        if (!TerminalCommitRules.IsTerminal(nameof(DurableJobState.Failed))) throw new InvalidOperationException("Failed must be terminal.");
        if (!TerminalCommitRules.IsTerminal(nameof(DurableJobState.Canceled))) throw new InvalidOperationException("Canceled must be terminal.");
        if (TerminalCommitRules.IsTerminal(nameof(DurableJobState.Running))) throw new InvalidOperationException("Running must not be terminal.");
        var now = DateTimeOffset.UtcNow;
        var a = new TerminalCommitReceipt("r", "b", "c", "j", "w", "wf", "Completed", "result", now, now, 1, 0, "OK", null);
        var b = a with { };
        if (!TerminalCommitRules.ReceiptMatches(a, b)) throw new InvalidOperationException("Identical receipt must be idempotent.");
        var conflict = a with { ResultFingerprint = "different" };
        if (TerminalCommitRules.ReceiptMatches(a, conflict)) throw new InvalidOperationException("Different result must conflict.");
        checks.Add("TERMINAL_STATES_PASS");
        checks.Add("IDENTICAL_RECEIPT_IDEMPOTENCY_PASS");
        checks.Add("DIFFERENT_RESULT_CONFLICT_PASS");
        return checks.AsReadOnly();
    }
}
