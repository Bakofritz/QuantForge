using System.Reflection;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class RecoveryContinuationGateArchitectureChecks
{
    public static IReadOnlyList<string> Run()
    {
        var source = File.ReadAllText(FindSource("RecoveryContinuationGateService.cs"));
        var continuation = File.ReadAllText(FindSource("CheckpointBoundContinuationService.cs"));
        var contracts = File.ReadAllText(FindSource("RecoveryContinuationContracts.cs"));
        var checks = new List<string>();
        Require(source.Contains("VerifyRecoveryBindingReconciliationAsync", StringComparison.Ordinal), checks, "Gate performs durable reconciliation read-back.");
        Require(source.Contains("ReconciliationBlocked", StringComparison.Ordinal), checks, "Gate has explicit blocked terminal-preparation state.");
        Require(source.Contains("ResearchJobLifecycle.Recover", StringComparison.Ordinal), checks, "Blocked continuation returns the job to RecoveryRequired.");
        Require(source.Contains("_leases.ReleaseAsync", StringComparison.Ordinal), checks, "Blocked continuation releases the recovery lease.");
        Require(continuation.Contains("RecoveryContinuationGateService", StringComparison.Ordinal), checks, "Checkpoint-bound continuation is gated before executing a step.");
        Require(!continuation.Contains("RecoveryPreflightBindingService(_preflightStore", StringComparison.Ordinal), checks, "Checkpoint continuation no longer performs an independent preflight-only gate.");
        Require(contracts.Contains("ReconciliationBlocked", StringComparison.Ordinal), checks, "Recovery continuation contract exposes reconciliation blocking.");
        return checks;
    }

    private static void Require(bool condition, List<string> checks, string description)
    {
        if (!condition) throw new InvalidOperationException("ARCHITECTURE_CHECK_FAILED: " + description);
        checks.Add("PASS: " + description);
    }

    private static string FindSource(string name)
    {
        var root = Directory.GetCurrentDirectory();
        var found = Directory.EnumerateFiles(root, name, SearchOption.AllDirectories).FirstOrDefault();
        if (found is null) throw new FileNotFoundException(name);
        return found;
    }
}
