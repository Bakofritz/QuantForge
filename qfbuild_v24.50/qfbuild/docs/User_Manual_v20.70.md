# QuantForge v20.70 User Manual

## Purpose
v20.70 establishes the boundary between architectural readiness and actual native runtime qualification and adds conservative resource admission for future controlled concurrency.

## Runtime Qualification
A runtime is qualified only when the capability manifest says native execution occurred and all supplied qualification checks pass. A compiled project or structural test does not qualify a runtime.

## Concurrency Admission
The resource gate requires:
1. native runtime qualification;
2. a positive per-case memory estimate;
3. a positive memory budget; and
4. estimated demand within the budget.

The gate only admits or rejects work. It does not execute cases and does not override recovery, lease, cancellation, or strategy-governance controls.

## Fail-Closed Conditions
- NATIVE_RUNTIME_NOT_QUALIFIED
- RESOURCE_ESTIMATE_UNAVAILABLE
- MEMORY_BUDGET_EXCEEDED

## Existing Recovery Controls
Prepared-only recovery remains non-replayable automatically. Reconciliation records, resolution records, and external evidence remain distinct from execution authority.
