# QuantForge v20.53 User Manual

## Recovery Binding Reconciliation Fault Validation

The new validation checks whether QuantForge safely handles failures while recording a recovery decision. It tests audit, evidence, and event persistence failures, retry behavior, duplicate records, conflicting records, and attempts to continue without a durable reconciliation.

A failed reconciliation record must never be treated as permission to continue recovery.

## Current limitation
Native .NET/SQLite execution is pending because the current build environment does not provide the required runtime.

## Deferred User Contributions
NT8/data contributions remain deferred until the native data-bridge phase. Codes: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, NT8 CONFIG.
