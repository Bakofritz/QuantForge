# QuantForge v20.60 User Manual

## What changed
When a continuation operation is durably marked `Prepared` but has no durable result, QuantForge now creates a reconciliation record explaining that the operation is ambiguous.

## What QuantForge does
1. Detects the `Prepared`-only operation.
2. Creates an evidence-bound reconciliation record if one does not already exist.
3. Stops the continuation with the fail-closed ambiguity code.
4. Requires an explicit reconciliation process before any future resolution.

## What QuantForge does not do
- It does not assume the callback failed.
- It does not assume the callback succeeded.
- It does not automatically rerun the callback.
- It does not treat a review record as permission to rerun external work.

## Operational meaning
A `Prepared` record means QuantForge cannot prove whether the callback reached completion before a crash. Treat the operation as requiring review. The reconciliation record provides the durable starting point for that review.

## Safety
The reconciliation mechanism is intentionally separate from live order authority and does not grant trading, execution, or arbitrary imported-code authority.
