using System.Text.RegularExpressions;

namespace QuantForge.ArchitectureTests;

public static class RecoveryBindingReconciliationArchitectureChecks
{
    public static bool HasReconciliationService()
        => File.Exists(FindSource("RecoveryBindingReconciliationService.cs"));

    public static bool HasFailClosedStates()
    {
        var text = File.ReadAllText(FindSource("RecoveryBindingReconciliationService.cs"));
        return text.Contains("BoundAndConsistent", StringComparison.Ordinal)
            && text.Contains("BlockedByBindingMismatch", StringComparison.Ordinal)
            && text.Contains("RecoveryMayContinue", StringComparison.Ordinal);
    }

    public static bool PersistsAuditAndEvidence()
    {
        var text = File.ReadAllText(FindSource("RecoveryBindingReconciliationService.cs"));
        return text.Contains("AppendRecoveryAuditAsync", StringComparison.Ordinal)
            && text.Contains("AppendEvidenceAsync", StringComparison.Ordinal)
            && text.Contains("RECOVERY_BINDING_RECONCILIATION", StringComparison.Ordinal);
    }

    public static bool RuntimeEntryPointExists()
    {
        var text = File.ReadAllText(FindSource("ResearchRuntime.cs"));
        return text.Contains("ReconcileRecoveryBindingAsync", StringComparison.Ordinal);
    }

    private static string FindSource(string fileName)
    {
        var root = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(root))
        {
            var candidate = Directory.GetFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
            if (candidate is not null) return candidate;
            var parent = Directory.GetParent(root)?.FullName;
            if (parent == root) break;
            root = parent ?? string.Empty;
        }
        return fileName;
    }
}
