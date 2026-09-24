# QuantForge New-Chat Handoff — v20.34

Continue from QuantForge Futures Research Platform v20.34.

## Current phase
Durable Continuation Receipt Reconciliation + Batch Accounting Integration.

## Architecture now
Case → Worker Identity → Lease → Heartbeat → Checkpoint → Controlled Continuation → Immutable Receipt → Batch Receipt Reconciliation → Derived Batch Accounting.

## Key v20.34 rule
Batch accounting is derived from immutable receipt history. Multiple receipts for the same ContextId are preserved, but only one latest terminal receipt contributes to batch totals. This prevents a failed/interrupted attempt and a later successful continuation from being double-counted.

## New components
- `BatchAccountingReconciler`
- `IResourceReceiptStore.LoadExecutionReceiptsForBatchAsync`
- SQLite batch receipt query
- `ResearchRuntime.ReconcileBatchAccountingAsync`
- automatic reconciliation after multi-case execution

## Safety
Parallel execution remains disabled. Live trading/order authority remains absent. Recovery requires validated fingerprints/checkpoint and a fresh lease. Visual refinement remains frozen in favor of core functionality.

## Validation limitation
No native .NET SDK/Android/Windows runtime is available in the current build environment. Source-level validation only; do not claim native runtime/device validation.

## Deferred User Contributions
Do not request user data prematurely. Use the established code words only when their validation stage is reached: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, NT8 CONFIG.

## Next build
v20.35 — Recovery/Batch Manifest Integrity + Terminal-State Agreement.
