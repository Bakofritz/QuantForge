# QuantForge — New-Chat Handoff v20.25

## Current release
**v20.25 — Account-Ledger Session Integrity + Trade Evidence Hardening**

Baseline: v20.24

## Current state
QuantForge is a native-first C#/.NET 10 LTS + .NET MAUI research platform targeting Windows and Android with shared core libraries and local-first evidence architecture.

The Master Continuous Build Directive remains active.

## v20.25 completed
1. Added sequenced immutable ledger accounting events.
2. Added explicit UTC calendar-day session keys.
3. Added `SESSION_STARTED` and `SESSION_ROLLED` evidence.
4. Added deterministic sequence-bound trade IDs to prevent same-timestamp collisions.
5. Added exactly-once daily-loss-lock transition evidence per UTC accounting day.
6. Added durable SQLite `ledger_trade_evidence` storage.
7. Added durable SQLite `ledger_accounting_events` storage.
8. Added immutable fingerprint conflict detection for persisted trade/event records.
9. Added research-runtime lifecycle evidence attachment for supported local evidence stores.
10. Extended architecture checks for session, trade identity, lock transition, and evidence sequencing.

## Critical accounting rule
The v20.25 session is explicitly a **UTC calendar-day accounting session**. Do not describe it as an exchange trading session until exchange-specific session calendars are implemented and validated.

## Critical identity rule
Trade identity is deterministic within an ordered ledger run. The trade sequence is included in the trade fingerprint and ID so identical economic fields at the same timestamp do not collapse into one trade.

## Evidence rule
Existing evidence keys are immutable. Re-submitting identical content is idempotent; submitting different content under the same immutable identity must raise an integrity conflict.

## Current execution/accounting chain
`MARKET EVENT`
→ causal timeframe observation
→ strategy / canonical position intent
→ protective exit resolution
→ risk gate
→ side-aware simulated fill
→ position state
→ account ledger
→ accounting events
→ durable evidence

## Safety boundary
Position intents and simulated fills are research operations, not broker orders. No live order or broker-routing authority exists.

## Fidelity rule
OHLCV does not provide observed intrabar event ordering. Tick/replay/bid/ask/spread fidelity requires the corresponding source data.

## Validation limitation
The current environment does not contain the .NET SDK/compiler, Android SDK/device, or Windows native runtime. Source/structural validation remains separate from native runtime validation.

## Visual status
Visual refinement remains intentionally frozen. Continue core functionality, data integrity, governance, persistence, research execution, and validation before returning to visual refinement.

## Deferred user contributions
Do not request them early.
- NT8 REPLAY
- NT8 TICK
- NT8 MINUTE
- NT8 DAILY
- REPLAY MATCH
- LATENCY DATA

## Next build
**v20.26 — Multi-Script / Multi-Instrument Research Isolation**

Continue autonomously from this handoff without asking the user to restate the architecture.
