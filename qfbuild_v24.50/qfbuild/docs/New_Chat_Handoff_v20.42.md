# QuantForge v20.42 — New-Chat Handoff

## Project
QuantForge Futures Research Platform / QuantForge Futures Backtester

## Current architecture
Native-first C#/.NET 10 LTS + .NET MAUI target for Windows and Android, shared research/domain libraries, local-first SQLite evidence/state storage, governed strategy acquisition, deterministic backtesting/optimization/robustness/Monte Carlo, worker/lease/checkpoint recovery, Internet Scout quarantine, and non-authoritative AI analysis.

## v20.42 completed
Durable Native Fault-Result Reconciliation + Failure-Recovery Invariants.

### New capability
The v20.41 native SQLite fault seam can now be followed by a controlled retry and durable-state verification for every supported storage fault point.

Fault points:
- LeaseAcquire
- LeaseRenew
- CheckpointWrite
- ReceiptWrite
- EvidenceWrite
- ValidationResultWrite
- JobStateWrite

### Native validation path
`StorageFaultInjector → SqliteLocalStore → injected exception → clean SqliteLocalStore retry → durable-state verification`

### Runtime entry point
`ResearchRuntime.ValidateNativeStorageFaultRecoveryAsync(databasePath)`

### Important limitation
The adapter is implemented against real SQLite, but the current build environment does not contain the .NET SDK/native runtime. Therefore native execution remains unclaimed until the project is run in an appropriate Windows/Android/.NET environment.

### Source correction
A prior source-level defect in `SqliteLocalStore.SaveDatasetAsync` was corrected: a lease-only `renewOnly` reference had been accidentally placed in dataset persistence. Lease fault injection now exists only in `MutateLeaseAsync`.

## Safety status
- Parallel research execution: disabled.
- Live trading/order authority: absent.
- AI remains non-authoritative.
- Imported strategy execution remains governed.
- Fault injection is validation-only.
- Visual refinement remains frozen while core functionality is prioritized.

## Next build
v20.43: bind storage fault invariants into worker/lease/recovery state transitions and test failure behavior around recovery continuation, terminal receipts, job states, and lease ownership.

## Required per-build artifacts
Continue producing:
- detailed User Manual
- standalone New-Chat Handoff
- Comprehensive Build Summary
- Release Manifest
- Validation Result
- Source Hash manifest

## Deferred contributions
Do not request user data yet. When a contribution becomes necessary, use the established short code words such as `NT8 REPLAY`, `NT8 TICK`, `NT8 MINUTE`, `NT8 DAILY`, `MES DATA`, `MNQ DATA`, `ROLL DATA`, `STRATEGY SRC`, `STRATEGY TEST`, `REPLAY MATCH`, `LATENCY DATA`, and `NT8 CONFIG`.
