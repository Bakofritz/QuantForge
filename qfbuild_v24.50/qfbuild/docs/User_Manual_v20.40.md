# QuantForge v20.40 — User Manual

## What changed
v20.40 adds a governed storage fault-validation boundary. It is intended for testing what happens when durable storage operations fail.

## Fault categories
- Lease acquisition
- Lease renewal
- Checkpoint write
- Execution receipt write
- Evidence write
- Concurrency validation-result write
- Research job-state write

## User-visible safety principle
Fault injection is a validation feature, not normal research execution. It must never be enabled implicitly.

## Validation interpretation
A result can say that the fault contract passed while also stating that native storage was not executed. These are different facts and must not be conflated.

## Concurrency
Parallel research execution remains disabled. v20.40 does not authorize simultaneous case execution.

## Research evidence
Fault-validation results receive deterministic fingerprints and are linked into the existing evidence/event chain.

## Deferred contributions
No user data is required for this build. Future NT8 and market-data contributions remain deferred until the relevant validation phase.
