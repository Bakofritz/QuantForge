# QuantForge User Manual v20.80

## Controlled-Concurrency Execution Boundary

v20.80 adds the boundary between concurrency design and future real execution. It checks native qualification, resource estimates, memory budget, worker identity, and deterministic lifecycle state before an execution adapter may proceed.

### Important
Passing the boundary does not itself execute a strategy, place an order, modify application settings, or replay an ambiguous recovery operation.

### Fail-closed conditions
- `NATIVE_RUNTIME_NOT_QUALIFIED`
- `RESOURCE_ESTIMATE_UNAVAILABLE`
- `MEMORY_BUDGET_EXCEEDED`
- `RESEARCH_LEASE_OWNERSHIP_LOST`

### Operational model
1. Recover and authorize the job.
2. Establish qualified runtime capability.
3. Calculate bounded resource demand.
4. Admit the requested concurrency.
5. Enter the deterministic lifecycle.
6. Verify lease ownership before/around future execution.
7. Produce durable terminal receipts through the existing terminal-commit layer.

## Current limitation
Native runtime qualification is not claimed until actual .NET/SQLite/Android execution is performed in an appropriate environment.
