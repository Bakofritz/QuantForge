# QuantForge User Manual — v20.30

## What changed
v20.30 adds durable resource evidence around multi-case research runs.

### Worker identity
Every multi-case run is associated with a worker identity derived from the device/worker identifier and runtime fingerprint. This does not grant additional authority; it records provenance.

### Execution receipts
Each completed or stopped case produces an execution receipt containing case/job identity, worker identity, result fingerprint, timing measurements, heartbeat renewals, resource status, and error code when applicable.

### Batch accounting
The system records the number of total, completed, failed, and canceled cases plus accumulated elapsed time and heartbeat renewals.

### Time limits
A case is bounded by `MaxCaseDuration`. A batch is bounded by `MaxBatchDuration`. User cancellation is recorded separately from resource timeout.

### Recovery
v20.30 does not automatically resume arbitrary work after a timeout. Recovery remains checkpoint- and fingerprint-bound.

## Current limitation
Parallel case execution is intentionally disabled. The current gate requires `MaxConcurrentCases == 1` until native runtime/resource validation is available.
