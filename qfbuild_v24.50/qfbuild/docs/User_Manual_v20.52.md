# QuantForge User Manual — v20.52

## Recovery Binding Reconciliation
When a worker attempts to recover a research job, QuantForge first checks the approved recovery preflight against the checkpoint and lease currently available.

### What the two outcomes mean
- **BoundAndConsistent:** the approved information and current state agree. This does not itself grant trading authority or bypass later continuation checks.
- **BlockedByBindingMismatch:** the information does not agree. Recovery must stop; QuantForge does not automatically repair the record.

### Runtime operation
`ReconcileRecoveryBindingAsync` performs the reconciliation and records an audit result. If evidence storage is enabled, the operation also records an evidence item and append-only event.

### Important limitation
The current development environment does not contain the .NET SDK/runtime. Native Windows/Android and SQLite execution therefore remain pending.

## Deferred Contributions
No data contribution is required for this build. When needed later, use the established short codes: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, NT8 CONFIG.
