# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.29

## 1. Build identity
- Release: v20.29
- Baseline: v20.28
- Focus: Resource Governance + Lease Heartbeat + Execution Receipts
- Native target: C# / .NET 10 LTS + .NET MAUI, Windows + Android
- Visual state: frozen by directive; no visual refinement added.

## 2. Engineering work
### Resource policy
Added `MultiCaseResourcePolicy` with explicit maximum case duration, batch duration, heartbeat interval, and concurrency gate. The current native coordinator deliberately permits only one active case.

### Lease heartbeat
Added a bounded heartbeat loop that renews the active case lease while execution is running. Renewal failure is treated as a research execution failure rather than allowing an unleased worker to continue silently.

### Case timeout
Case execution receives a linked cancellation token and is stopped when the declared maximum case duration is exceeded. Timeout is distinguished from user cancellation.

### Execution receipts
Each case now produces a receipt containing case identity, result fingerprint, resource status, elapsed time, heartbeat renewal count, state, and error information. Receipt integrity is fingerprinted and recorded through the existing evidence/event store.

### Runtime integration
`ResearchRuntime.ExecuteMultiCaseAsync` now accepts the resource policy and forwards it to the coordinator.

## 3. Design tradeoffs
- Resource governance is declarative and enforceable at the orchestration boundary; it does not claim OS-level CPU/RAM quotas that are unavailable through the current abstraction.
- Lease renewal is deliberately conservative. A failed renewal stops the case rather than risking execution after authority expiry.
- Receipts preserve individual case lineage and are not merged into mutable batch state.
- Parallel execution remains explicitly gated. `MaxConcurrentCases` must currently equal 1. This prevents unvalidated concurrent SQLite/resource behavior from being presented as production-ready.

## 4. Validation
Source-level validation performed:
- all C# files structurally balanced;
- resource policy and validation contract present;
- heartbeat renewal uses the existing `IJobLeaseStore.RenewAsync` contract;
- case timeout creates a linked cancellation boundary;
- execution receipts are fingerprinted;
- receipt evidence/event recording uses the existing append-only evidence interface;
- ResearchRuntime passes the policy through to the coordinator;
- concurrency remains explicitly gated at one case.

Native compile/device validation remains unavailable because the environment does not provide the .NET SDK/compiler or Android/Windows SDK/runtime.

## 5. Safety
No live trading, live orders, arbitrary imported-code execution, canonical-data mutation, unrestricted AI application mutation, or broker authority was added.

## 6. Data fidelity
The current imported research boundary remains declared OHLCV. This build does not upgrade data to tick, bid/ask, or replay fidelity.

## 7. Deferred user contributions
No contribution is required for v20.29. NT8 and market-data contribution codes remain deferred until the relevant native data-validation phase.

## 8. Next phase — v20.30
- durable resource receipts/checkpoint linkage;
- batch accounting for resource consumption;
- explicit worker identity and stale-worker handling;
- bounded concurrency design review;
- native runtime validation when toolchain is available.
