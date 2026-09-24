# QuantForge v20.33 User Manual

## Controlled recovery continuation
When a worker disappears, QuantForge does not blindly restart the case. v20.32 validates the job, lease, cancellation state, checkpoint, and immutable research fingerprints. v20.33 adds the execution bridge: a validated checkpoint is supplied to a deterministic continuation operation.

## What happens
1. Recovery validation runs.
2. A fresh lease is acquired.
3. The validated checkpoint becomes the continuation cursor.
4. The research operation performs one bounded step.
5. QuantForge rejects cursor regression or missing state hash.
6. A new checkpoint is durably stored.
7. The lease is renewed.
8. When the operation reports completion, the job becomes Completed and completion evidence is recorded.

## What it does not do
It does not invent missing instructions, replay arbitrary operations, enable live trading, or enable parallel execution.

## Deferred User Contributions
None required for this build. Future data contributions remain gated by their relevant validation phase and short code words.
