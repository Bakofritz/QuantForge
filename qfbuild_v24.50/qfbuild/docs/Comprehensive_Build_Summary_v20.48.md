# QuantForge v20.48 — Recovery Preflight Evidence + Reconciliation Receipt Binding

## Purpose
Record the recovery decision before controlled checkpoint continuation so an allowed or blocked recovery is permanently traceable to the worker identity, integrity result, and any already-known terminal receipt.

## Changes
- Added `RecoveryPreflightReceipt` and `IRecoveryPreflightStore`.
- Added durable SQLite `recovery_preflight_receipts` table.
- Added immutable/idempotent SQLite persistence and retrieval for recovery preflight receipts.
- Added `RecoveryPreflightService`.
- Recovery preflight records `Allowed` or `Blocked` without mutating job state.
- Preflight fingerprints bind batch, context, job, worker/device identity, decision, reason, integrity fingerprint, and known terminal receipt fingerprint.
- Added recovery audit/evidence/event linkage for the preflight decision.
- Integrated preflight recording into `RecoveryContinuationService` before checkpoint validation/lease acquisition.
- Propagated preflight storage into `CheckpointBoundContinuationService`.
- Added `ResearchRuntime.LoadRecoveryPreflightsAsync` for durable inspection.
- Added architecture checks for contract, SQLite persistence, runtime wiring, and continuation integration.

## Safety boundary
The preflight recorder is observational and evidentiary. It does not repair conflicts, invent receipts, acquire leases, or resume jobs. `Allowed` means the existing read-only integrity gate found no blocking condition at that moment; continuation still performs its own checkpoint and lease checks.

## Validation
- v20.47 baseline: 120 C# files.
- v20.48 total: 124 C# files; 98 source C# files and 26 architecture-test files.
- Structural brace validation: PASS across all C# files.
- Native .NET runtime: unavailable in the current environment.
- Native SQLite execution: not executed.
- Parallel research remains disabled.
- Live trading/order authority remains absent.
- Visual refinement remains deferred.

## Next build
v20.49 — Recovery Preflight Reconciliation + Lease/Checkpoint Decision Binding.
