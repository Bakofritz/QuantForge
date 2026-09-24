# QuantForge New-Chat Handoff v22.40

Continue from v22.40. The release consolidates the following additions:


## Permanent release rule
Every future release must include a simple bullet-point summary of the live-environment state and capabilities.

## Current safety boundary
- Research/backtesting: available independently.
- Live market-data observation: separate from order authority.
- Broker session: must be authenticated and identity-bound.
- Session liveness: must remain valid; stale sessions are blocked.
- Audit and risk state: must remain healthy.
- Live pathway: explicitly armed and continuously revalidated.
- Order routing: blocked when any required condition becomes stale or invalid.
- Unknown executions: blocked pending reconciliation.
- Production live-money execution: not implemented or claimed.

## Next logical work
Continue consolidating compatible reliability, provenance, recovery, authentication, audit, broker-state, and adversarial validation work while keeping production live-money execution separately controlled.
