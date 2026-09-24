# QuantForge Futures Research Platform — User Manual v20.55

## What v20.55 adds
QuantForge now places a final safety checkpoint between recovery preparation and actual resumed research execution.

### Recovery sequence
1. QuantForge checks the job, worker, terminal receipts, cancellation state, checkpoint, and lease.
2. It validates that the recovery preflight still matches the checkpoint and lease.
3. It creates a recovery binding reconciliation.
4. The reconciliation is stored atomically as audit + evidence + event when native SQLite storage is available.
5. QuantForge reads the records back from storage.
6. Only when the reconciliation is both logically allowed and durably present can checkpoint-bound work begin.
7. If anything fails, continuation is blocked and the job remains in/reverts to `RecoveryRequired`.

## What this means for users
A recovered job cannot silently resume merely because an in-memory validation passed. The durable record of the recovery decision must exist first.

## Safety boundaries
- Recovery is controlled research execution only.
- No live order authority exists.
- AI does not gain unrestricted application mutation authority.
- Canonical market data is not silently changed by recovery.
- Failed reconciliation does not authorize continuation.

## Current validation limitation
The source architecture has been structurally checked, but the current environment does not contain the .NET SDK/runtime. Therefore this release does not claim native compile or device execution validation.

## User Contributions — Deferred Until Needed
No contribution is needed now. When native NT8/data validation reaches the appropriate stage, use the established short code words rather than preparing large datasets prematurely.
