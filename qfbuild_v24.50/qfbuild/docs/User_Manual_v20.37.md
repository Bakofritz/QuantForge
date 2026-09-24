# QuantForge v20.37 — User Manual

## Concurrency Safety Gate
Use the concurrency safety gate to evaluate whether a research batch has the architectural prerequisites for future controlled parallel execution.

## Resource Budget
The gate accepts maximum elapsed time, heartbeat renewals, concurrent cases, and estimated memory budget. These are governance limits, not measurements of actual device capacity.

## Contention Domains
The review explicitly records policies for SQLite, evidence storage, lease storage, batch accounting, checkpoints, and isolated case state.

## Important
A passing architecture review does not enable parallel execution. `ParallelExecutionEnabled` remains false until native Windows/Android runtime and resource behavior have been validated.

## Heartbeats
A lease-renewal failure is surfaced as an execution failure unless the caller itself requested cancellation.

## Recovery
Continue using: reconcile worker/lease → validate checkpoint and immutable fingerprints → acquire fresh lease → continue from validated checkpoint → record evidence.

## User Contributions
None are required for v20.37.
