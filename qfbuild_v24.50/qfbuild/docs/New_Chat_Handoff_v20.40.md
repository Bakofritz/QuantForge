# QuantForge v20.40 — New-Chat Handoff

Resume the QuantForge Master Build from **v20.40**.

## Current phase
Native Validation Result Reconciliation + Storage Fault-Injection Boundary.

## Current architecture
Manifest → Case → Job → Worker → Lease → Heartbeat → Checkpoint → Execution → Receipt → Recovery → Continuation → Batch Accounting → Terminal-State Agreement → Concurrency Safety Gate → Concurrency Validation Harness → SQLite Contention Adapter → Fault-Injection Boundary.

## Key safety state
- Parallel research execution: DISABLED.
- Live trading/order authority: NOT PRESENT.
- AI remains non-authoritative.
- Visual refinement is frozen; core functionality has priority.
- Native .NET/Android/Windows runtime validation is not available in the current build environment.

## v20.40 additions
- StorageFaultPoint.
- StorageFaultPlan.
- StorageFaultInjector.
- StorageFaultInjectedException.
- StorageFaultInjectionHarness.
- StorageFaultValidationService.
- ResearchRuntime.ValidateStorageFaultBoundaryAsync().
- Architecture checks.

## Important distinction
The current fault harness validates the explicit fault contract in a dependency-light environment. It does not claim that native SQLite operations have been fault-injected or that device runtime behavior has passed.

## Next build
v20.41: bind the fault-injection boundary to actual storage operations through a test-only/injected storage seam; preserve production behavior and the disabled concurrency gate.

## Deferred user contributions
No contribution is needed for v20.40. Use established code words only when the corresponding data/validation stage is reached: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, NT8 CONFIG.
