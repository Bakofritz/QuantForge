# QuantForge v20.75 User Manual

## What this build does
Provides the internal lifecycle model used to govern multi-case research work before execution is permitted.

## Lifecycle
1. Pending: declared but not admitted.
2. Admitted: worker identity and admission boundary have been established.
3. Running: execution may be represented by an external execution layer.
4. CancelRequested: cancellation has been requested and completion must not silently override it.
5. Completed/Failed/Canceled: terminal states.

## Important limitation
This component does not itself run strategies, place orders, access live trading, or enable parallel execution. It is a safety/control layer.

## Deterministic aggregation
Terminal cases are ordered by ContextId and fingerprinted. Arrival order does not change the aggregate fingerprint.

## Recovery interaction
The lifecycle must remain downstream of the recovery/reconciliation gates. An ambiguous recovery state must never be converted into automatic execution merely because a case is Pending or Admitted.
