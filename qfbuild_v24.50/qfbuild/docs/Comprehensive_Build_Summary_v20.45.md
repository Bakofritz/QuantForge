# QuantForge v20.45 — Transactional Terminal Fault-Execution Harness

## Build purpose
v20.45 validates the new v20.44 terminal transaction boundary against controlled failures. The objective is to prove the intended fail-closed behavior: if any protected terminal write fails, the terminal outcome must not survive as a partial durable result; a later clean retry must be able to commit and the complete terminal state must then be verifiable.

## Implemented
- Added `TransactionalTerminalFaultPoint` with five terminal-transaction fault locations:
  - JobStateWrite
  - ReceiptWrite
  - EvidenceWrite
  - EventWrite
  - BeforeCommit
- Extended the test-only/native-capable `StorageFaultInjector` seam with terminal-event and pre-commit fault points.
- Added terminal-event fault injection immediately before the event insert.
- Added pre-commit fault injection immediately before `SQLiteTransaction.CommitAsync`.
- Added `SqliteTransactionalTerminalFaultAdapter`.
- Each validation case:
  1. creates a running job and valid worker lease;
  2. injects one controlled failure inside the terminal transaction;
  3. requires the expected injected exception;
  4. verifies the failed transaction left the job Running and did not leave the terminal receipt, evidence, or event;
  5. retries the same logical terminal commit without fault injection;
  6. verifies Completed job state, receipt, evidence, and event.
- Added `TransactionalTerminalFaultValidationService` and `ResearchRuntime.ValidateTransactionalTerminalFaultAsync`.
- Added architecture checks for all transactional fault points and runtime integration.
- Kept the ordinary storage fault harness limited to its seven non-terminal standard points; terminal transaction faults are exercised by the dedicated transactional harness.

## Engineering rationale
The important distinction is between detecting a partial write and proving that the database transaction prevents one. v20.44 established the transaction boundary. v20.45 adds a controlled test seam at each meaningful terminal phase so the platform can verify the rollback contract when a native runtime is available.

The `BeforeCommit` fault is especially important because it simulates an interruption after all protected writes have been prepared but before the transaction is committed. The expected result is still a complete rollback.

The harness deliberately performs a clean retry after each injected failure. This tests both sides of the recovery rule:
- failed commit does not become a false success;
- controlled retry can establish the durable terminal outcome.

## Safety boundary
This is a validation-only capability. It has no live trading authority, no order submission authority, no provider write authority, and no unrestricted AI mutation authority.

## Validation performed in this environment
- All C# files structurally balanced: PASS.
- New transactional source files structurally balanced: PASS.
- Runtime method and architecture integration present: PASS.
- Native .NET/SQLite execution: NOT AVAILABLE because `dotnet` is not installed in the current environment.
- Therefore the transactional harness was not executed natively here and its `NativeStorageExecuted` result field is an implementation contract, not an executed test result from this environment.
- Android/Windows device execution: not performed.
- Parallel research: disabled.
- Visual refinement: deferred.

## Source inventory
- 92 source C# files.
- 113 total C# files including architecture tests.

## Known limitations
- Actual rollback behavior still requires native execution with Microsoft.Data.Sqlite.
- Provider-neutral atomicity is only guaranteed where the storage provider implements `ITerminalCommitStore` with equivalent transactional semantics.
- Cross-device database sharing remains prohibited; each device retains its own local database and governed synchronization remains the intended model.

## Next build
v20.46 — Transactional Commit Idempotency + Duplicate Terminal Outcome Protection.
Focus:
- same terminal commit retried after a successful commit must be idempotent;
- same receipt fingerprint with identical content is accepted as the same outcome;
- same identity with conflicting terminal content is rejected;
- duplicate terminal outcomes cannot be counted twice;
- recovery/reconciliation must recognize an already committed terminal outcome without creating another terminal result.

## User Contributions — Deferred Until Needed
No user contribution is required for v20.45. NT8 REPLAY/TICK/MINUTE/DAILY and MES/MNQ/ROLL data remain deferred until the native data bridge and triangulation stages.
