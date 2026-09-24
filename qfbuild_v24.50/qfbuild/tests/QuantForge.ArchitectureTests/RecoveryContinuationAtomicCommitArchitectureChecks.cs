namespace QuantForge.ArchitectureTests;

public static class RecoveryContinuationAtomicCommitArchitectureChecks
{
    public static void AssertArchitecture(string sourceRoot)
    {
        var service = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Runtime/CheckpointBoundContinuationService.cs"));
        var contracts = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Storage/RecoveryContinuationReplayContracts.cs"));
        var sqlite = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Storage/SqliteLocalStore.cs"));
        var harness = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Runtime/RecoveryContinuationAtomicCommitHarness.cs"));
        Require(contracts.Contains("IRecoveryContinuationAtomicCommitStore", StringComparison.Ordinal), "ATOMIC_COMMIT_CONTRACT_MISSING");
        Require(service.Contains("RECOVERY_CONTINUATION_REQUIRES_ATOMIC_RESULT_CHECKPOINT_STORE", StringComparison.Ordinal), "NON_ATOMIC_STORE_MUST_FAIL_CLOSED");
        Require(service.Contains("CommitResultAndCheckpointAsync", StringComparison.Ordinal), "ATOMIC_COMMIT_CALL_MISSING");
        Require(!service.Contains("await _replayStore.RecordResultAsync", StringComparison.Ordinal), "SEPARATE_RESULT_WRITE_MUST_NOT_PRECEDE_ATOMIC_COMMIT");
        Require(!service.Contains("await _jobs.SaveCheckpointAsync(checkpointToSave", StringComparison.Ordinal), "SEPARATE_CHECKPOINT_WRITE_MUST_NOT_REPLACE_ATOMIC_COMMIT");
        Require(sqlite.Contains("BeginTransactionAsync", StringComparison.Ordinal), "SQLITE_TRANSACTION_MISSING");
        Require(sqlite.Contains("UPDATE recovery_continuation_operations SET state=$state", StringComparison.Ordinal), "ATOMIC_RESULT_UPDATE_MISSING");
        Require(sqlite.Contains("INSERT INTO research_checkpoints", StringComparison.Ordinal), "ATOMIC_CHECKPOINT_INSERT_MISSING");
        Require(sqlite.Contains("CommitAsync", StringComparison.Ordinal), "ATOMIC_COMMIT_FINALIZATION_MISSING");
        Require(sqlite.Contains("RollbackAsync", StringComparison.Ordinal), "ATOMIC_ROLLBACK_MISSING");
        Require(harness.Contains("CHECKPOINT_CONFLICT_ROLLS_BACK_RESULT_UPDATE", StringComparison.Ordinal), "ROLLBACK_CONFLICT_CASE_MISSING");
        Require(harness.Contains("ATOMIC_COMMIT_FAILURE_ROLLS_BACK_BOTH_MUTATIONS", StringComparison.Ordinal), "ROLLBACK_FAILURE_CASE_MISSING");
        Require(harness.Contains("NON_ATOMIC_STORE_IS_REJECTED", StringComparison.Ordinal), "NON_ATOMIC_CASE_MISSING");
        Require(service.IndexOf("ValidateBeforeCommitAsync", StringComparison.Ordinal) < service.IndexOf("CommitResultAndCheckpointAsync", StringComparison.Ordinal), "PRE_COMMIT_REVALIDATION_MUST_PRECEDE_ATOMIC_COMMIT");
    }

    private static void Require(bool condition, string code)
    {
        if (!condition) throw new InvalidOperationException(code);
    }
}
