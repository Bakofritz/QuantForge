# QuantForge v20.31 User Manual

## Worker/Lease Reconciliation
A worker identity records the execution environment. A lease grants temporary ownership of a research job. A receipt records the durable execution outcome. v20.31 adds reconciliation to determine whether ownership is still active, stale, or already associated with a durable terminal receipt.

## Recovery
If a lease is active, do not reclaim it. If it is expired, QuantForge records `StaleWorker` and requires checkpoint and fingerprint validation before reacquisition. A receipt already associated with a terminal job is treated as committed evidence.

## Current limitations
Parallel execution is disabled. Native compilation/device validation is pending toolchain availability. Recovery does not guess an arbitrary instruction boundary.
