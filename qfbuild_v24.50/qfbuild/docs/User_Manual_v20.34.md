# QuantForge User Manual — v20.34

## What changed
v20.34 makes multi-case batch resource accounting resilient to recovery. QuantForge can retain every execution receipt while counting only the final terminal receipt for each case when producing batch totals.

## Concepts
- **Case:** one isolated research workload.
- **Worker:** execution identity.
- **Lease:** temporary execution ownership.
- **Heartbeat:** periodic proof that the worker remains active and renews its lease.
- **Checkpoint:** durable resumable research position.
- **Receipt:** immutable evidence of an execution attempt/outcome.
- **Batch accounting:** derived summary of case outcomes and resource consumption.

## Recovery example
1. Worker A starts Case 7.
2. Worker A produces an execution attempt receipt.
3. Worker A disappears and its lease expires.
4. QuantForge validates the recovery checkpoint and required fingerprints.
5. Worker B receives a fresh lease.
6. Worker B continues the case and produces a terminal completion receipt.
7. Batch reconciliation sees both receipts but counts only the terminal receipt for Case 7.
8. The earlier receipt remains available as audit history.

## Batch accounting
Totals are derived from receipt history rather than trusting a mutable in-memory counter. Multiple workers are represented explicitly when a batch has receipts from more than one worker identity.

## Current limitations
Parallel execution is not enabled. Native Windows/Android compilation and device validation have not been performed in the current environment. Visual refinement is intentionally deferred.

## Safety
Research remains governed and non-live. No live trading authority is present. External code remains subject to the acquisition/quarantine/scrubbing workflow established in earlier builds.

## User Contributions — Deferred Until Needed
No contribution is required for v20.34. When the appropriate validation phase arrives, use the established code words rather than sending large data prematurely.
