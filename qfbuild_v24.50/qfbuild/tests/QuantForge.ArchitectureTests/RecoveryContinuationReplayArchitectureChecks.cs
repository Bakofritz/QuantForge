namespace QuantForge.ArchitectureTests;

public static class RecoveryContinuationReplayArchitectureChecks
{
    public static void AssertArchitecture(string sourceRoot)
    {
        var service = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Runtime/CheckpointBoundContinuationService.cs"));
        var store = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Storage/RecoveryContinuationReplayContracts.cs"));
        var sqlite = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Storage/SqliteLocalStore.cs"));
        var harness = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Runtime/RecoveryContinuationReplayHarness.cs"));
        Require(service.Contains("RecoveryContinuationReplayFingerprint.For", StringComparison.Ordinal), "OPERATION_FINGERPRINT_MISSING");
        Require(service.Contains("PrepareAsync", StringComparison.Ordinal), "PREPARE_LEDGER_WRITE_MISSING");
        Require(service.Contains("RecordResultAsync", StringComparison.Ordinal), "RESULT_LEDGER_WRITE_MISSING");
        Require(service.Contains("RECOVERY_CONTINUATION_AMBIGUOUS_IN_FLIGHT_OPERATION", StringComparison.Ordinal), "AMBIGUOUS_REPLAY_MUST_FAIL_CLOSED");
        Require(service.Contains("operation.State == RecoveryContinuationOperationState.Prepared", StringComparison.Ordinal), "PREPARED_REPLAY_CHECK_MISSING");
        Require(service.Contains("RecoveryContinuationExecutionGate.ExecuteAsync", StringComparison.Ordinal), "EXECUTION_GATE_MISSING");
        Require(store.Contains("IRecoveryContinuationReplayStore", StringComparison.Ordinal), "REPLAY_STORE_CONTRACT_MISSING");
        Require(sqlite.Contains("recovery_continuation_operations", StringComparison.Ordinal), "REPLAY_TABLE_MISSING");
        Require(harness.Contains("RESULT_RECORDED_REPLAY_SKIPS_CALLBACK", StringComparison.Ordinal), "REPLAY_SKIP_CASE_MISSING");
        Require(harness.Contains("PREPARED_REPLAY_IS_AMBIGUOUS_AND_BLOCKED", StringComparison.Ordinal), "AMBIGUOUS_CASE_MISSING");
        Require(service.IndexOf("PrepareAsync", StringComparison.Ordinal) < service.IndexOf("RecoveryContinuationExecutionGate.ExecuteAsync", StringComparison.Ordinal), "PREPARE_MUST_PRECEDE_CALLBACK");
        Require(service.IndexOf("RecordResultAsync", StringComparison.Ordinal) < service.IndexOf("SaveCheckpointAsync", StringComparison.Ordinal), "RESULT_RECORD_MUST_PRECEDE_CHECKPOINT_COMMIT");
    }

    private static void Require(bool condition, string code)
    {
        if (!condition) throw new InvalidOperationException(code);
    }
}
