# QuantForge User Manual — v20.54

## Native Recovery Reconciliation Fault Validation

QuantForge now tests the recovery-reconciliation persistence boundary using the real SQLite storage layer rather than only a model-only test store.

### What is being protected?
Before a paused or interrupted research job can be considered eligible for controlled recovery, QuantForge must verify that the saved checkpoint and lease still match the recovery request. The reconciliation decision is recorded as an audit record, evidence record, and research event.

### What happens if saving fails?
The three records are committed as one SQLite transaction. If the audit, evidence, or event write is deliberately faulted during validation, the transaction is rolled back. QuantForge does not receive a successful reconciliation result from that failed transaction.

### Retry behavior
A clean retry can establish the complete durable record. Repeating the exact same reconciliation is treated as the same immutable logical result. A conflicting identity is rejected rather than overwriting the existing record.

### User-facing safety rule
A detected recovery mismatch or an incomplete reconciliation is never treated as permission to continue. Recovery remains governed by the durable state and explicit continuation checks.

## Current validation status
The source and architecture checks pass structurally. Native .NET/SQLite execution is still pending because the current build environment does not contain the required .NET SDK/runtime.

## Visual status
Visual refinement is intentionally deferred while core application functionality, storage integrity, recovery, data fidelity, and governance are prioritized.

## Deferred User Contributions
No contribution is needed now. NT8/data contributions will be requested only when the corresponding validation phase is reached.

Code words:
- NT8 REPLAY
- NT8 TICK
- NT8 MINUTE
- NT8 DAILY
- MES DATA
- MNQ DATA
- ROLL DATA
- STRATEGY SRC
- STRATEGY TEST
- REPLAY MATCH
- LATENCY DATA
- NT8 CONFIG
