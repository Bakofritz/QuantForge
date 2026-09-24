# QuantForge User Manual — v20.44

## What changed
QuantForge now has a transactional terminal research commit boundary for SQLite-backed execution.

## Why it matters
A research case should never appear to have a successful terminal receipt when the corresponding terminal job state was not durably committed. The SQLite implementation now commits the terminal state, receipt, evidence, and terminal event together.

## Normal workflow
1. A case acquires a lease.
2. The worker runs the governed research operation.
3. The worker verifies lease ownership before terminalization.
4. QuantForge creates the terminal job state and immutable receipt.
5. SQLite commits the terminal state, receipt, evidence, and event in one transaction.
6. Only after the transaction succeeds is the outcome treated as durable.
7. The lease is then released.

## If a storage fault occurs
The transaction is rolled back. The case must not be reported as durably terminal merely because execution reached the end of the research computation.

## Important terms
- Worker: execution identity.
- Lease: temporary ownership of a mutable research case.
- Receipt: durable proof of an execution outcome.
- Checkpoint: resumable progress marker.
- Transactional terminal commit: one storage transaction for the terminal job state and its associated receipt/evidence/event.

## Current limitations
Native .NET/SQLite execution has not been performed in the current environment. Parallel research remains disabled. Live trading remains unavailable. Visual refinement is deferred while core functionality is completed.

## User contributions
None required for this build. NT8/data contributions will be requested only when the relevant validation phase is reached.
