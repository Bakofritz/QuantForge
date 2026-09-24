# QuantForge User Manual v22.50

## What changed


## Live-environment state
- Research/backtesting: available independently.
- Live market-data observation: separate from order authority.
- Broker session: must be authenticated and identity-bound.
- Session liveness: must remain valid; stale sessions are blocked.
- Audit and risk state: must remain healthy.
- Live pathway: explicitly armed and continuously revalidated.
- Order routing: blocked when any required condition becomes stale or invalid.
- Unknown executions: blocked pending reconciliation.
- Production live-money execution: not implemented or claimed.
- Audit startup verification: must be fresh and tied to the current provenance context.
- Replayed or stale verification evidence cannot clear recovery.
- Broker state evidence: must carry the same execution identity and request fingerprint before it can become definitive.

## Operating principles
1. Research and backtesting authority is separate from live authority.
2. Observation capability does not grant order-routing capability.
3. Material environment changes require revalidation.
4. Unknown broker outcomes require reconciliation before continuation.
5. Security, audit, risk, broker-session, and recovery gates fail closed.

## Validation limitations
Native broker transport and production reconciliation are not claimed; deterministic evidence-binding architecture checks are included.
