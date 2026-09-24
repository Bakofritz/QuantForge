# QuantForge v20.43 — Recovery-State Failure Integrity

## Objective
Bind the storage-fault/recovery work into durable job, lease, checkpoint, and terminal-receipt invariants so a storage failure cannot be interpreted as successful research progress.

## Implemented
- Terminal receipt ordering corrected: durable terminal job state is written before terminal receipt publication.
- Failure/cancellation receipt ordering corrected: durable Failed/Canceled job state is written before terminal receipt publication.
- Active lease ownership is checked immediately before terminal job mutation.
- Read-only `RecoveryStateIntegrityService` validates durable job/lease/receipt/cancellation agreement.
- Detects eight integrity classes: failed recovery marked completed; terminal receipt without terminal job; mutation with invalid lease; checkpoint-after-persistence-failure boundary; duplicate terminal outcomes; recovery after cancellation; lease ownership change; successful result without durable terminal state.
- Runtime entry point: `ValidateRecoveryStateIntegrityAsync`.
- No automatic repair/resume is performed by the validator; ambiguous state fails closed.

## Important tradeoff
The current storage interfaces do not expose one cross-table transaction spanning job state, receipts, evidence, and lease tables. v20.43 therefore uses safe ordering plus explicit invariant validation rather than claiming atomic cross-table commit. Native SQLite transaction-level integration remains a later phase.

## Validation
- Source structural brace validation performed for all C# files.
- Native .NET/SQLite runtime execution remains unavailable in this environment because `dotnet` is not installed.
- No live trading/order authority introduced.
- Parallel research remains disabled.
- Visual refinement remains deferred while core functionality is prioritized.

## Next build
v20.44 — Transactional Terminal Commit Boundary / Recovery Integrity Harness: introduce a storage-level atomic commit boundary where supported, plus deterministic fault tests around terminal state + receipt publication.
