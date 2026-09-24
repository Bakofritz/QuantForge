# QuantForge v20.47 — Worker Recovery Integrity

## Purpose
Strengthen worker recovery so a worker cannot resume a case when durable job state, terminal receipts, cancellation, or lease ownership disagree.

## Changes
- Added `WorkerRecoveryIntegrityService` as a read-only recovery preflight gate.
- Detects multiple terminal receipts for one job.
- Detects a terminal receipt while the job is nonterminal.
- Detects a terminal job without a terminal receipt.
- Blocks recovery after durable cancellation.
- Blocks recovery while another worker owns an unexpired lease.
- Explicitly identifies states eligible for controlled recovery.
- Integrated the gate into `RecoveryContinuationService` before checkpoint/release logic.
- Added `ResearchRuntime.ValidateWorkerRecoveryIntegrityAsync`.
- Added architecture checks for the new recovery states and receipt reconciliation.

## Safety boundary
The validator is read-only. It never invents a missing receipt, repairs a conflict, or resumes execution. Conflicts remain blocked for explicit reconciliation.

## Validation
- 118 C# files at v20.46 baseline; 120 after v20.47 implementation.
- Structural brace validation performed across all C# files.
- Native .NET/SQLite/Android/Windows runtime execution unavailable in this environment.
- Parallel research remains disabled.
- Live trading/order authority remains absent.
- Visual refinement remains deferred.

## Next build
v20.48 — Recovery Preflight Evidence + Reconciliation Receipt Binding.
