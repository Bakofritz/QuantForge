# QuantForge v20.32 — Controlled Recovery Continuation + Checkpoint Validation

## Purpose
Connect worker/lease reconciliation to durable checkpoints so a replacement worker can continue only after explicit validation.

## Implemented
- RecoveryContinuationRequest/Result contracts.
- RecoveryContinuationService.
- Durable job, lease, receipt, cancellation, and checkpoint validation gates.
- Dataset/configuration/engine fingerprint matching.
- Fresh lease acquisition before continuation.
- Running jobs with stale/no active lease are first transitioned through RecoveryRequired before being restarted.
- Recovery continuation audit and evidence records.
- Runtime integration via PrepareRecoveryContinuationAsync.
- Architecture checks.

## Safety boundary
This build does not resume arbitrary instructions. It returns a validated checkpoint boundary and establishes a fresh lease; the execution layer must consume the returned checkpoint explicitly. Parallel execution remains disabled. No live trading/order authority is introduced.

## Validation limitations
Native .NET compilation, Android/Windows device execution, and full integration testing are unavailable in the current build environment. Source-level validation is therefore explicitly distinguished from runtime validation.
