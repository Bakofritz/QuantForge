# QuantForge v20.36 — User Manual

## Terminal-State Agreement
Use the terminal-state agreement review to compare each manifest case's durable job state with its terminal execution receipt. A healthy case has one coherent terminal outcome when the job is terminal. Multiple terminal receipts or mismatched job/receipt states require reconciliation and are not silently corrected.

## Controlled Concurrency Review
The controlled-concurrency review is a design/integrity check. It verifies case/job uniqueness, input fingerprint consistency, worker identity completeness, and terminal outcome uniqueness. It also records that native runtime/resource validation is still required.

## Important
A successful readiness review does not enable parallel execution. In v20.36 `ParallelExecutionEnabled` remains false.

## Recovery
Recovery still follows: reconcile worker/lease → validate checkpoint and immutable fingerprints → acquire a fresh lease → continue from a validated checkpoint → record continuation evidence.

## User Contributions
None are required for this build.
