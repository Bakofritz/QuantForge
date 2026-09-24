# QuantForge Master Build v20.75
## Consolidated Controlled-Concurrency Lifecycle Foundation (v20.71-v20.75)

### Purpose
This milestone consolidates the planned admission, lease/lifecycle, cancellation, heartbeat/resource accounting, terminal-state, and deterministic aggregation work into one storage-agnostic lifecycle layer.

### Architectural boundary
The lifecycle is a state machine only. It does not execute user code, acquire real leases, write SQLite, or enable parallel execution. Those remain external, separately qualified boundaries.

### State model
Pending -> Admitted -> Running -> Completed/Failed/Canceled.
Running/Admitted may enter CancelRequested. Terminal states are immutable; repeated cancellation requests on Canceled are idempotent.

### Safety properties
- Worker identity is required before admission.
- Cases cannot start before admission.
- Terminal results require non-negative resource accounting.
- A canceled case cannot be reported as completed through the lifecycle state machine.
- Batch aggregation rejects duplicate contexts.
- Batch aggregation rejects non-terminal cases.
- Aggregate ordering is deterministic by ContextId and state fingerprint.
- No parallel execution authority is introduced.

### Consolidated internal milestones
- v20.71 admission-to-running transition contract
- v20.72 cancellation and terminality contract
- v20.73 heartbeat/resource accounting representation
- v20.74 deterministic terminal aggregation boundary
- v20.75 lifecycle validation harness and integration seam

### Validation limitations
Native .NET execution was not available in the build environment. Native SQLite, Android, and device-runtime validation are not claimed. Structural source inspection and deterministic harness design validation are included.
