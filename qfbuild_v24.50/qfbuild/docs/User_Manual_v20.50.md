# QuantForge User Manual — v20.50

## Recovery Preflight Binding
Before a recovered research case continues, QuantForge can verify that the saved recovery decision still matches the checkpoint and worker lease actually present.

### What it checks
1. An allowed recovery preflight exists for the worker and job.
2. The current checkpoint exists.
3. Checkpoint sequence, cursor, and state hash match the approved recovery record.
4. The current lease exists.
5. Lease device, version, fingerprint, and expiry remain valid.

### If a check fails
QuantForge stops recovery rather than guessing or changing historical records.

### Native validation status
This release contains source-level and architecture validation. Native .NET/SQLite execution is pending a suitable runtime environment.

### User contributions
None required for this release. NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, and NT8 CONFIG remain deferred until their relevant phase.
