# QuantForge User Manual — v20.38

## What v20.38 does
v20.38 adds a controlled test harness for concurrency-related invariants. It does not enable parallel research execution.

## Harness checks
- Lease race: confirms only one simulated owner acquires a lease.
- Receipt idempotency: confirms repeated identical receipt writes converge to one logical record.
- Receipt conflict: confirms contradictory data for an existing receipt identity is rejected.
- Checkpoint monotonicity: confirms a lower checkpoint cannot replace a higher accepted checkpoint.
- Cancellation race: confirms cancellation does not also report completion.
- Aggregate ordering: confirms case aggregation is duplicate-free and deterministic.

## How to invoke
The native runtime exposes `ValidateConcurrencyHarnessAsync()` through `ResearchRuntime` when the application integrates this release.

## Interpretation
A PASS means the modeled invariant passed. It is not proof of production SQLite, Android, Windows, CPU, memory, scheduler, or I/O behavior.

## Safety
Parallel execution remains disabled. No live trading authority is introduced.
