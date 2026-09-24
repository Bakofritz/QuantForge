using System;
using System.IO;
using System.Linq;
using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class WorkerRecoveryIntegrityArchitectureChecks
{
    public static void Validate()
    {
        var source = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "QuantForge.Runtime", "WorkerRecoveryIntegrityService.cs"));
        if (!source.Contains("TerminalOutcomeConflict", StringComparison.Ordinal)) throw new InvalidOperationException("Recovery conflict state missing.");
        if (!source.Contains("CancellationRequested", StringComparison.Ordinal)) throw new InvalidOperationException("Cancellation recovery gate missing.");
        if (!source.Contains("LoadExecutionReceiptsForJobAsync", StringComparison.Ordinal)) throw new InvalidOperationException("Receipt reconciliation missing.");
        if (!source.Contains("SafeToRecover", StringComparison.Ordinal)) throw new InvalidOperationException("Safe recovery state missing.");
        var enumNames = Enum.GetNames<WorkerRecoveryIntegrityState>();
        if (enumNames.Length != 6) throw new InvalidOperationException("Unexpected recovery integrity state count.");
    }
}
