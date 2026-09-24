# QuantForge v20.37 — Comprehensive Build Summary

## Release
v20.37 — Concurrency Safety Gate + Resource/Storage Contention Design

## Purpose
Formalize the shared-resource boundaries that must be respected before controlled parallel research execution can ever be enabled. Parallel execution remains disabled.

## Implemented
- ConcurrencyResourceBudget with validation.
- Explicit contention domains for SQLite, immutable evidence, leases, batch accounting, checkpoints, and per-case state.
- ConcurrencySafetyGateService performing manifest/job/receipt/resource checks.
- Deterministic resource-policy fingerprint and gate fingerprint.
- ResearchRuntime EvaluateConcurrencySafetyAsync entry point.
- Architecture checks for resource budgets, contention domains, explicit parallel gate, and gate fingerprint.
- Heartbeat renewal failures are no longer silently treated as cancellation-only exceptions; they are surfaced when the caller did not cancel.

## Safety boundaries
- MaxConcurrentCases remains 1.
- Native Windows/Android runtime and resource behavior are still required before parallel execution.
- No live trading/order authority was added.
- No canonical data mutation or unrestricted AI authority was added.

## Validation
- 78 C# files.
- Structural brace-balance validation: PASS.
- Native dotnet/device validation unavailable in current environment and not claimed.

## Deferred User Contributions
None required for this build. Future NT8/MES/MNQ/strategy/replay/latency contributions remain deferred until their relevant validation stages.
