# QuantForge User Manual v20.32

## Controlled Recovery
When a worker disappears, QuantForge first reconciles the lease and receipt state. A recovery continuation can only be prepared when no active lease remains, no terminal receipt exists, no durable cancellation is pending, a checkpoint exists, and dataset/configuration/engine fingerprints match.

## What Prepare Recovery Does
1. Loads the durable job.
2. Checks active lease ownership.
3. Checks terminal execution receipts.
4. Checks cancellation requests.
5. Loads the latest checkpoint.
6. Validates checkpoint/job/request fingerprints.
7. Acquires a new lease.
8. Moves a recoverable job through RecoveryRequired to Running.
9. Records an immutable recovery audit/evidence event.
10. Returns the validated checkpoint cursor and state hash to the caller.

## What It Does Not Do
It does not guess which instructions ran before failure, does not bypass fingerprints, does not resume a terminal job, and does not enable live trading or parallel execution.

## Deferred User Contributions
No user contribution is needed for this build. NT8/MES/MNQ/strategy/replay/latency samples remain deferred until their relevant validation stages.
