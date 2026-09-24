# QuantForge Futures Research Platform — Comprehensive Build Summary v20.58

## Release
- **Release:** v20.58
- **Build:** Continuation Commit Idempotency and Crash-Replay Boundary Harness
- **Parent:** v20.57 Recovery Continuation Crash-Window and Post-Authorization Invalidation Harness
- **Primary objective:** make the post-callback/pre-checkpoint crash window explicit, durable, replay-safe, and fail-closed.

## What changed
v20.57 protected the continuation path from authorization becoming stale. v20.58 adds a durable operation ledger around each continuation step.

### 1. Deterministic operation fingerprint
`RecoveryContinuationReplayFingerprint` derives a stable operation identity from the batch/context/job/worker fingerprints, expected research fingerprints, and the current checkpoint cursor.

The fingerprint identifies the exact continuation operation being attempted. It does not grant authority and does not replace the recovery authorization or invalidation checks.

### 2. Durable operation preparation
Before the caller's continuation callback is invoked, QuantForge records a `Prepared` operation in `recovery_continuation_operations`.

This creates an explicit durable marker for an operation that may have been interrupted.

### 3. Durable result recording
After the callback returns successfully, QuantForge records the returned cursor, state hash, completion flag, and optional result fingerprint as `ResultRecorded` **before** the checkpoint is persisted.

If the process subsequently crashes before checkpoint persistence, the next recovery can recognize the completed deterministic result and commit the already-recorded result without invoking the callback a second time.

### 4. Replay recognition
If an operation with the same fingerprint is already `ResultRecorded`, the continuation service reconstructs the step from the durable result and skips the continuation callback.

The normal `ValidateBeforeCommitAsync` guard still runs before checkpoint persistence, so replay recognition does not bypass recovery authority.

### 5. Ambiguous in-flight operations fail closed
If an operation is found in `Prepared` state without a durable result, QuantForge treats the outcome as ambiguous. It does **not** automatically invoke the callback again.

This is intentional. A crash between callback completion and durable result recording cannot be proven to have left the callback unexecuted. Automatically replaying such an operation could duplicate non-idempotent work. The safe response is to block and require a new controlled reconciliation decision.

### 6. Duplicate checkpoint protection
The existing SQLite checkpoint store already treats an existing `(job_id, sequence)` checkpoint as immutable: an identical write is accepted idempotently, while a conflicting write is rejected. v20.58 places the continuation result ledger before that checkpoint boundary, giving replay a durable result identity instead of relying only on the checkpoint row.

## Required execution sequence
The protected continuation sequence is now:

`recovery authorization -> execution gate -> pre-step invalidation check -> operation fingerprint -> durable Prepared record -> continuation callback -> durable ResultRecorded record -> pre-commit invalidation check -> idempotent checkpoint commit`

A recognized `ResultRecorded` replay follows:

`recovery authorization -> execution gate -> pre-step invalidation check -> replay lookup -> reconstruct recorded step -> pre-commit invalidation check -> idempotent checkpoint commit`

## Safety model
- No live-trading authority is introduced.
- The callback remains caller-supplied.
- A prepared-but-unresolved operation is never silently replayed.
- A conflicting operation fingerprint or result is rejected.
- Replay does not bypass lease, cancellation, job-state, or checkpoint revalidation.
- Checkpoint persistence remains immutable by job/sequence.
- No automatic authority escalation or cancellation clearing is introduced.

## Validation
- C# structural brace balance: **PASS**
- Replay-store contract presence: **PASS**
- Durable replay table and persistence methods present: **PASS**
- Operation fingerprint wired before callback: **PASS**
- Result record wired before checkpoint save: **PASS**
- Prepared replay fail-closed path present: **PASS**
- Result-recorded replay skip path present: **PASS**
- Deterministic replay harness present: **PASS**
- Native .NET execution: **UNAVAILABLE in this environment**
- Native SQLite execution: **NOT CLAIMED**
- Parallel research: **DISABLED**
- Live trading/order authority: **DISABLED**
- Visual refinement: **DEFERRED**

## Source inventory
- **149 C# files** in the v20.58 package.
- New v20.58 runtime files:
  - `RecoveryContinuationReplayFingerprint.cs`
  - `RecoveryContinuationReplayHarness.cs`
- New v20.58 storage contract:
  - `RecoveryContinuationReplayContracts.cs`
- Updated storage implementation:
  - `SqliteLocalStore.cs`
- Updated continuation service:
  - `CheckpointBoundContinuationService.cs`
- New architecture test:
  - `RecoveryContinuationReplayArchitectureChecks.cs`

## Residual boundary
The ledger intentionally distinguishes two cases:
1. **ResultRecorded:** safe to replay the durable result without calling the callback again.
2. **Prepared only:** callback completion is ambiguous; automatic replay is prohibited.

This prevents a false exactly-once guarantee at the one boundary that cannot be resolved solely by application-level code without making the callback itself part of the same durable transaction.

## Next build
**v20.59 — Continuation Result/Checkpoint Atomicity Adapter and Reconciliation Boundary Harness**

The next stage should investigate whether the durable result record and checkpoint advancement can be made atomic for supported native stores, while retaining the fail-closed `Prepared` behavior for stores or callbacks that cannot participate in an atomic transaction.
