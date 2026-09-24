# QuantForge v20.65 User Manual — Recovery Reconciliation Consolidation

## Purpose
v20.65 provides a controlled way to record and validate reconciliation of an interrupted continuation operation without turning ambiguous state into automatic code replay.

## Normal Recovery
1. QuantForge validates the recovery request.
2. Durable binding/reconciliation is read back.
3. The continuation execution gate authorizes the normal path.
4. The continuation operation is prepared.
5. The callback executes only through the existing execution gate.
6. Result and checkpoint are committed through the atomic storage seam.

## Prepared-Only Recovery
If the durable operation is `Prepared` but no result exists:
- QuantForge records or reuses a durable review record.
- The operation remains stopped.
- No callback replay is automatically performed.
- An external reconciliation process may supply evidence, but evidence alone does not invoke the callback.

## Resolution Records
Resolution records are bound to:
- operation fingerprint;
- reconciliation decision fingerprint;
- evidence fingerprint;
- resolver fingerprint;
- issue/expiry timestamps;
- consumption state.

Expired, stale, conflicting or already-consumed records are rejected.

## External-Side-Effect Evidence
External evidence identifies what happened outside the durable QuantForge continuation ledger. It does not become execution authority merely because it exists.

## Operator Guidance
Treat `Prepared-only`, `stale`, `expired`, `conflicting`, and `replay requested` states as controlled reconciliation states. Do not interpret a review record as an instruction to execute an imported strategy or arbitrary callback.

## Current Qualification
Structural validation is included. Native .NET/SQLite and Android/device qualification must still be performed in an appropriate runtime environment.
