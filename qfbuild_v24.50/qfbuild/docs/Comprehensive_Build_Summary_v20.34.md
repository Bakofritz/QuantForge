# QuantForge v20.34 — Durable Continuation Receipt Reconciliation + Batch Accounting Integration

## Purpose
v20.34 makes batch resource accounting derived from the durable execution-receipt history. This prevents an interrupted attempt followed by a successful recovery/continuation from being counted as two completed cases.

## Implemented
- Added `BatchAccountingReconciler`.
- Added durable batch receipt retrieval by `BatchId`.
- Reconciliation groups immutable execution receipts by `ContextId` and counts one latest terminal receipt per case.
- Preserves all receipt history; reconciliation changes only the derived batch accounting record.
- Derived totals include completed, failed, canceled, elapsed milliseconds, and heartbeat renewals.
- Multi-worker accounting is represented explicitly as `MULTI_WORKER` with a deterministic worker-fingerprint aggregate.
- Batch record fingerprint is derived from immutable batch inputs, status, totals, and counted receipt fingerprints rather than wall-clock time.
- Runtime exposes `ReconcileBatchAccountingAsync`.
- Multi-case execution invokes receipt-derived reconciliation after its normal batch-record write.
- Existing visual refinement remains frozen.

## Important behavior
A case may have multiple execution receipts across interruption/recovery. Only one terminal receipt for that context is counted in batch accounting. Earlier receipts remain immutable audit history.

## Safety boundaries
- Parallel execution remains disabled.
- No live orders/trading authority was added.
- No unrestricted AI mutation authority was added.
- Recovery still requires checkpoint/fingerprint validation and a fresh lease.

## Validation
- 83 C# files.
- Structural brace-balance validation: PASS.
- New batch receipt retrieval contract implemented by SQLite store.
- Reconciler source integrated with runtime and multi-case coordinator.
- Native .NET compilation/device validation unavailable in current environment and therefore not claimed.

## Next
v20.35 should harden recovery/batch reconciliation around explicit case manifests and terminal-state agreement, then perform a controlled design review for eventual concurrency validation.
