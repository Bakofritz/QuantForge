# QuantForge User Manual — v20.58

## Purpose
v20.58 protects the small but important crash window after a continuation step has finished and before its checkpoint is saved.

In plain language: QuantForge remembers which exact continuation operation it was performing and, when possible, remembers the completed result before moving the checkpoint forward.

## How a continuation step works
1. Recovery authorization is established.
2. The execution gate confirms the authorization is still valid.
3. The pre-step invalidation guard checks the durable job, cancellation, lease, and checkpoint.
4. QuantForge creates a deterministic operation fingerprint.
5. The operation is durably recorded as `Prepared`.
6. The continuation callback runs.
7. The returned result is durably recorded as `ResultRecorded`.
8. The pre-commit invalidation guard runs again.
9. The checkpoint is saved.

## What happens after a crash?
### Crash after `ResultRecorded` but before checkpoint save
On the next controlled recovery, QuantForge recognizes the same operation fingerprint, reconstructs the already-recorded result, and does **not** invoke the continuation callback a second time. The normal pre-commit safety checks still run before the checkpoint is advanced.

### Crash while the operation is only `Prepared`
QuantForge cannot prove whether the callback completed before the crash. It therefore blocks the operation rather than automatically running the callback again.

This is a deliberate safety behavior. It avoids claiming exactly-once execution where the system does not have enough durable evidence to prove it.

## What does the operation fingerprint mean?
It identifies the continuation operation using the research context, worker identity, expected dataset/configuration/engine fingerprints, and the current checkpoint cursor.

It is an identity and replay-recognition mechanism. It is not an execution permission.

## What if a duplicate result is submitted?
The durable replay ledger requires the result to match the operation that was prepared. A conflicting cursor, state hash, completion state, or result fingerprint is rejected.

## What about duplicate checkpoints?
The checkpoint store treats an existing job/sequence pair as immutable. Repeating the exact same checkpoint is idempotent; attempting to write a different value for the same sequence is rejected.

## Safety limitations
This build does not:
- enable live trading;
- submit broker orders;
- bypass recovery authorization;
- bypass cancellation, lease, or checkpoint validation;
- automatically replay an ambiguous in-flight continuation;
- execute arbitrary imported code;
- add parallel research authority.

## Validation limitation
The package includes deterministic structural and architecture validation. Native .NET and native SQLite execution are not available in the current build environment, so this release does not claim native runtime execution.

## Operational guidance
A `Prepared`-only operation should be treated as an unresolved recovery boundary. The correct next step is controlled reconciliation rather than manually bypassing the replay ledger or directly calling the continuation callback.
