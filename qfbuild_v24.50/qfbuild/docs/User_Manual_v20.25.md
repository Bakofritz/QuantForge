# QuantForge User Manual v20.25

## Release purpose
v20.25 strengthens research accounting and evidence integrity. It does not add live trading.

## What the ledger now records
For each research lifecycle, the ledger can produce ordered accounting events containing:
- UTC timestamp;
- UTC calendar-day session key;
- event type;
- position transition;
- realized and daily realized P&L;
- equity;
- daily-loss-lock state;
- reference/details;
- fingerprint.

## Session handling
The current accounting session is a UTC calendar day. Crossing midnight UTC rolls the daily accounting values and records `SESSION_ROLLED`. An open position is not automatically closed solely because the date changes.

This is not yet an exchange-session calendar.

## Trade IDs
Trade IDs are deterministic and sequence-bound. This allows two otherwise identical trades occurring at the same timestamp to remain separate research records.

## Daily loss lock
When realized daily loss reaches or exceeds the configured loss limit, the ledger records one `DAILY_LOSS_LOCKED` transition event for that UTC accounting day. Future entries are rejected by the research risk gate until the accounting day rolls.

## Durable evidence
SQLite can persist trade evidence and ledger accounting events. Identical immutable records are safe to re-submit. A conflicting fingerprint for an existing immutable identity is rejected rather than silently overwritten.

## Research-runtime attachment
The runtime can attach a completed position-lifecycle result to the evidence ledger and persist its trades/accounting events when the local evidence store implements the ledger evidence contract.

## Data fidelity
OHLCV-based simulation remains an explicit model. It does not imply tick sequencing, bid/ask, spread, or replay fidelity.

## Safety
- Research only.
- No live broker orders.
- No broker routing.
- No arbitrary imported-code execution.
- AI remains non-authoritative.
- Canonical data remains governed.

## Deferred contributions
Use the established codes only when the relevant workflow requests them: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, REPLAY MATCH, LATENCY DATA.
