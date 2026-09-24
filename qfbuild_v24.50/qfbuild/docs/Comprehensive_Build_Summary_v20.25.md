# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.25

## 1. Build identity
- Release: **v20.25 — Account-Ledger Session Integrity + Trade Evidence Hardening**
- Baseline: v20.24
- Product: QuantForge Futures Research Platform
- Primary architecture: C# / .NET 10 LTS + .NET MAUI, shared research core for Windows and Android
- Research-only: **yes**
- Live trading: **disabled**
- Visual refinement: **frozen/deferred by user direction**

## 2. Objective
v20.25 hardens the account ledger as an auditable research accounting boundary. The release adds deterministic session/event sequencing, collision-resistant deterministic trade identifiers for same-timestamp trades, explicit daily-loss-lock transition evidence, and durable SQLite evidence tables.

Canonical accounting chain:

`Session Boundary → Position Open → Position/Trade Close → Daily P&L → Equity → Risk Lock Transition → Immutable Evidence`

The accounting session definition in this release is **UTC calendar day**. It is not claimed to be the exchange's trading session schedule. Exchange/session-calendar semantics remain future work.

## 3. Implemented
### 3.1 Ledger accounting events
`AccountLedger` now emits immutable in-memory accounting events with:
- monotonic sequence number;
- UTC timestamp;
- explicit UTC calendar-day session key;
- event type;
- position before/after;
- realized P&L;
- daily realized P&L;
- equity;
- daily-loss-lock state;
- reference ID;
- details;
- deterministic fingerprint.

Events currently include `SESSION_STARTED`, `SESSION_ROLLED`, `POSITION_OPENED`, `TRADE_CLOSED`, and `DAILY_LOSS_LOCKED`.

### 3.2 Deterministic trade identity
Trade fingerprints now include a deterministic per-ledger trade sequence. Trade IDs use the sequence plus a fingerprint prefix. This prevents identical trades occurring at the same timestamp from receiving the same ID while remaining reproducible for the same ordered research run.

### 3.3 Daily-loss-lock transition evidence
The ledger records `DAILY_LOSS_LOCKED` exactly once per UTC accounting day when realized daily loss crosses the configured limit. The transition is reset for the next UTC accounting day.

The lock remains a simulated research risk boundary; it does not represent broker-side enforcement.

### 3.4 Session rollover integrity
A UTC date transition resets daily realized P&L and the session peak and records a `SESSION_ROLLED` event. If a research position spans the UTC boundary, the position is not silently closed merely because the accounting day changes.

### 3.5 Durable SQLite trade/evidence storage
Added:
- `ledger_trade_evidence`
- `ledger_accounting_events`

The persistence layer is idempotent for an identical existing fingerprint and rejects an attempted mutation of an existing immutable trade/event record with a conflicting fingerprint.

### 3.6 Research-runtime evidence attachment
`ResearchRuntime.RecordPositionLifecycleEvidenceAsync` records the lifecycle evidence hash, then persists trade and accounting-event records when the configured local evidence store supports `ILedgerEvidenceStore`, followed by an append-only research event.

## 4. Engineering decisions
1. **UTC calendar day is explicit.** This prevents the application from falsely equating a simple date boundary with an exchange-specific session calendar.
2. **Trade sequence is part of the trade fingerprint.** Hashing only economic fields could collide for identical same-timestamp trades. Ordered sequence makes identity deterministic and unique within a ledger run.
3. **Immutable persistence rejects mutation.** `INSERT OR IGNORE` alone could hide a changed record. v20.25 first checks the existing fingerprint and raises an integrity conflict when the same key is presented with different content.
4. **Daily lock is a transition, not merely a boolean.** The evidence chain records when the lock became active.
5. **No second accounting engine was introduced.** The existing `AccountLedger` remains the single research accounting authority.

## 5. Validation
Source-level validation targets:
- C# structural balance across all source files: PASS
- deterministic sequence-bound trade IDs: PASS by source/architecture checks
- same-timestamp trade uniqueness: PASS by architecture test definition
- session-start and session-roll events: PASS by architecture test definition
- daily-loss-lock transition event uniqueness: PASS by architecture test definition
- immutable SQLite evidence conflict checks: PASS by source inspection
- lifecycle evidence binder present: PASS
- live/broker authority boundary retained: PASS

Native validation:
- .NET SDK/compiler: unavailable in current environment
- Android SDK/device: unavailable
- Windows native runtime: unavailable

Therefore this release does **not** claim native compilation, Android installation, Windows execution, or device testing. Behavioral tests remain source-level/architecture checks until a native toolchain is available.

## 6. Safety and authority boundary
No live orders, broker routing, arbitrary source execution, application-setting mutation, canonical market-data mutation, or unrestricted AI mutation were added. QuantForge remains a governed research simulator.

## 7. Data fidelity
The ledger can account for declared OHLCV-derived simulated fills. It does not create tick ordering, bid/ask, spread, or replay fidelity. Intrabar ambiguity remains governed by the declared OHLCV policy. Native NT8 validation remains dependent on the corresponding source data.

## 8. Deferred User Contributions
Nothing is requested at this stage.

When the relevant workflow reaches native-data validation, use:
- **NT8 REPLAY** — native Market Replay sample
- **NT8 TICK** — NT8 tick export
- **NT8 MINUTE** — NT8 minute export
- **NT8 DAILY** — NT8 daily export
- **REPLAY MATCH** — paired replay/export validation sample
- **LATENCY DATA** — measured network/provider observations

These remain intentionally deferred until the appropriate build stage.

## 9. Next engineering phase — v20.26
Focus: **Multi-Script / Multi-Instrument Research Isolation**.

Planned work:
- explicit research context identity per script/instrument/timeframe;
- isolated position/ledger state per research context;
- deterministic aggregation across multiple independent contexts;
- shared immutable dataset references without shared mutable ledger state;
- multi-script optimization boundaries;
- evidence lineage linking each result to its isolated context;
- preparation for simultaneous Windows + Android research jobs under existing leases.

The visual layer remains frozen while core functionality continues.
