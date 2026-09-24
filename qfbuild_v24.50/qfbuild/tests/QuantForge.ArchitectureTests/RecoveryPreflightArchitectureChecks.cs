using System;
using System.IO;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class RecoveryPreflightArchitectureChecks
{
    public static void Validate()
    {
        var contracts = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "QuantForge.Storage", "RecoveryPreflightContracts.cs"));
        var service = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "QuantForge.Runtime", "RecoveryPreflightService.cs"));
        var sqlite = File.ReadAllText(Path.Combine("..", "..", "..", "..", "src", "QuantForge.Storage", "SqliteLocalStore.cs"));
        if (!contracts.Contains("RecoveryPreflightReceipt", StringComparison.Ordinal)) throw new InvalidOperationException("Recovery preflight receipt contract missing.");
        if (!contracts.Contains("IRecoveryPreflightStore", StringComparison.Ordinal)) throw new InvalidOperationException("Recovery preflight store contract missing.");
        if (!service.Contains("Decision", StringComparison.Ordinal) || !service.Contains("TerminalReceiptFingerprint", StringComparison.Ordinal)) throw new InvalidOperationException("Preflight decision binding missing.");
        if (!service.Contains("RECOVERY_PREFLIGHT", StringComparison.Ordinal)) throw new InvalidOperationException("Recovery preflight audit/evidence type missing.");
        if (!sqlite.Contains("recovery_preflight_receipts", StringComparison.Ordinal)) throw new InvalidOperationException("SQLite recovery preflight table missing.");
        if (!sqlite.Contains("AppendRecoveryPreflightAsync", StringComparison.Ordinal)) throw new InvalidOperationException("SQLite recovery preflight persistence missing.");
        if (!service.Contains("SafeToRecover", StringComparison.Ordinal) || !service.Contains("Blocked", StringComparison.Ordinal)) throw new InvalidOperationException("Allowed/blocked decision boundary missing.");
    }
}
