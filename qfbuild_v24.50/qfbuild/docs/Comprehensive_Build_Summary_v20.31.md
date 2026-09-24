# QuantForge v20.31 — Durable Worker/Lease Reconciliation + Recovery Audit

## Purpose
Strengthen recovery without guessing execution progress. v20.31 reconciles durable job state, execution receipts, and lease ownership, records stale-worker findings, and preserves an immutable recovery audit trail.

## Changes
- Added lease inspection (`ILeaseInspectionStore` / SQLite implementation).
- Added receipt-by-job query for reconciliation.
- Added `WorkerReconciliationService`.
- Added recovery states: NoAction, LeaseActive, StaleWorker, ReceiptAlreadyCommitted, RecoveryRequired.
- Added durable `recovery_audits` table with immutable conflict detection.
- Added evidence/event linkage for reconciliation audits.
- Corrected v20.30 coordinator token ordering so batch checkpoint loading uses the batch timeout token after it is created.
- Preserved sequential execution gate; parallel execution remains disabled.

## Recovery rule
Expired ownership never means automatic arbitrary resume. QuantForge records the stale-worker condition, then checkpoint/configuration/dataset/engine fingerprints must be validated before a new lease and controlled continuation.

## Validation
Source structural validation: all C# files balanced. Native .NET/Android/Windows compilation and device tests are unavailable in the current environment and are not claimed.
