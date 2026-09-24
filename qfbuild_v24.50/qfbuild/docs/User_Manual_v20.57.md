# QuantForge User Manual — v20.57

## Purpose
v20.57 strengthens recovery continuation safety. In plain language: even after QuantForge says a recovery job is allowed to continue, it checks the important conditions again immediately before running the next continuation step and again immediately before saving the next checkpoint.

This protects against changes that happen in the small time window between authorization and execution.

## What is checked again?
At both boundaries QuantForge checks:
1. The recovery authorization is still `Ready`.
2. The job still exists and is still `Running`.
3. No durable cancellation request has appeared.
4. The recovery lease still exists.
5. The lease still belongs to the requesting worker.
6. The lease has not expired.
7. The checkpoint still exists.
8. The checkpoint sequence, cursor, and state hash still match the checkpoint that was authorized.

## What happens if something changes?
QuantForge fails closed. It stops the continuation rather than trying to repair the situation automatically.

Examples:
- If the lease disappears, continuation stops.
- If a cancellation request appears, continuation stops.
- If another process changes the job state, continuation stops.
- If the checkpoint is replaced, continuation stops.

The system does not silently ignore these conditions and does not automatically retry the continuation.

## Why are there two checks?
The first check protects the moment immediately before the continuation callback.

The second check protects the moment immediately before the resulting checkpoint is committed.

This distinction matters because a condition can change while a continuation step is being calculated. The second check prevents an old authorization from being used to advance durable recovery state after that change.

## User-facing effect
Normal users should not need to operate the guard directly. It is an internal safety layer around recovery continuation.

If recovery is blocked, the recorded failure code identifies the invalidation reason. The relevant durable job, lease, cancellation, and checkpoint records remain the source of truth.

## Validation limitation
The package contains deterministic structural and fault-injection validation. Native .NET and native SQLite execution are not available in the current build environment, so this release does not claim native runtime execution.

## Scope limitations
This build does not:
- enable live trading;
- submit broker orders;
- create new market-data authority;
- automatically execute arbitrary imported scripts;
- invent research instructions;
- add parallel research authority.

## Recommended operational interpretation
Treat a blocked continuation as a requirement for a fresh recovery assessment. Do not bypass the guard by calling the continuation callback directly in production integrations.
