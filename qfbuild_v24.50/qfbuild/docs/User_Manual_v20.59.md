# QuantForge User Manual — v20.59

## Purpose
v20.59 makes the durable continuation result and checkpoint advancement an atomic storage operation for supported native stores.

In plain language: after a continuation step finishes, QuantForge no longer saves the result and checkpoint as two separate durable actions when the native store supports the new atomic boundary.

## Normal continuation sequence
1. Recovery authorization is established.
2. The execution gate confirms authorization.
3. The pre-step invalidation guard checks the durable state.
4. QuantForge creates the deterministic operation fingerprint.
5. The operation is recorded as `Prepared`.
6. The continuation callback runs.
7. The pre-commit invalidation guard runs.
8. QuantForge atomically records the result and advances the checkpoint.

## Replay sequence
If the operation is already `ResultRecorded` but its checkpoint needs reconciliation:
1. Recovery authorization is re-established.
2. The execution gate and pre-step guard run.
3. The recorded result is loaded.
4. The callback is skipped.
5. The pre-commit guard runs.
6. The recorded result and checkpoint are reconciled through the atomic storage seam.

## What does atomic mean here?
It means the durable replay-result update and checkpoint write are committed inside one SQLite transaction. If either part fails, the transaction is rolled back.

It does **not** mean that arbitrary code executed by the continuation callback is transactional or reversible.

## What happens if the callback already ran but the process crashed?
If the durable operation remains only `Prepared`, QuantForge still cannot prove whether the callback completed. It therefore does not automatically execute the callback again.

If a result was already durably recorded, QuantForge can reconcile that recorded result without invoking the callback a second time.

## What happens if the checkpoint conflicts?
The atomic transaction rejects the conflict. The result transition and checkpoint mutation are rolled back together.

## What happens with a non-atomic storage implementation?
The continuation service rejects it with `RECOVERY_CONTINUATION_REQUIRES_ATOMIC_RESULT_CHECKPOINT_STORE`. v20.59 does not silently fall back to the older two-write sequence.

## Safety limitations
This build does not:
- enable live trading;
- submit broker orders;
- bypass recovery authorization;
- bypass cancellation, lease, or checkpoint validation;
- automatically replay a `Prepared`-only operation;
- claim transactionality for external callback side effects;
- execute arbitrary imported code;
- add parallel research authority.

## Validation limitation
Structural and architecture validation passed. Native .NET and native SQLite execution are not available in the current build environment, so this release does not claim native runtime execution.

## Operational guidance
Treat a `Prepared`-only operation as an unresolved recovery boundary. Do not manually call the callback to "see if it works." The next planned Master Build introduces an explicit durable reconciliation decision path for that condition.
