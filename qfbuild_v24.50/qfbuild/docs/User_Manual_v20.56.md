# QuantForge User Manual — v20.56

## Recovery Continuation Fault-Injection Harness

v20.56 adds a final execution-side safety seam between a durable recovery authorization result and the continuation callback supplied by the recovery caller.

### What the gate does
`RecoveryContinuationExecutionGate` accepts a recovery authorization result and a continuation callback. The callback is permitted only when the authorization state is exactly `Ready`.

Any other recovery state is blocked with:

`RECOVERY_CONTINUATION_NOT_AUTHORIZED`

The gate does not convert a blocked result into an authorized result, does not retry a failed callback, and does not create trading authority.

### Harness scenarios
The deterministic harness checks:

- **Blocked authorization:** callback execution count must remain zero.
- **Reconciliation read-back failure:** represented by a blocked authorization; callback execution count must remain zero.
- **Authorized continuation:** callback execution count must be exactly one.
- **Continuation callback failure:** the injected failure propagates and the callback is not automatically retried.

### How this relates to v20.55
v20.55 established the durable reconciliation continuation gate: reconciliation is persisted, read back, and verified before recovery can be authorized. v20.56 adds a second explicit seam so that the continuation delegate itself is still protected by the authorization result.

### User-facing interpretation
A recovery operation should be considered executable only after the system reports a `Ready` recovery authorization. A blocked result is not an invitation to retry execution manually through an alternate path.

### Safety status
Live trading and order submission remain unavailable. Parallel multi-case execution remains disabled pending native runtime/resource validation and controlled concurrency review. No automatic recovery repair is introduced.

### Validation limitation
This package was structurally validated because the current build environment lacks the .NET SDK/runtime. Native .NET and SQLite execution are not claimed.
