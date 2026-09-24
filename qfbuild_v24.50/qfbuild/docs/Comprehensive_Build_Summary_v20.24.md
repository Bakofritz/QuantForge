# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.24

## 1. Build identity
- Release: **v20.24 — Canonical Position Execution Lifecycle**
- Baseline: v20.23
- Product: QuantForge Futures Research Platform
- Primary architecture: C# / .NET 10 LTS + .NET MAUI, shared research core for Windows and Android
- Research-only: **yes**
- Live trading: **disabled**
- Visual refinement: **frozen/deferred by user direction**

## 2. Objective
v20.24 binds the canonical signed position-intent model into a deterministic position lifecycle. The goal is to make long, short, exit, and reversal semantics explicit at the ledger boundary without introducing broker or order-routing authority.

Canonical chain:

`Market Event → Strategy Observation → Position Intent → Protective Exit → Risk Gate → Side-Aware Fill → Position → Ledger → Evidence`

A reversal is explicitly modeled as:

`existing position close → ledger accounting → risk gate → new opposite-side entry`

The runner never submits a broker order.

## 3. Implemented
### 3.1 Canonical position lifecycle runner
Added `CanonicalPositionLifecycleRunner`.

It supports:
- Hold
- Enter Long
- Enter Short
- Exit
- Reverse to Long
- Reverse to Short
- legacy long-only strategy adaptation
- signed position quantities
- one protective exit per position/event
- strategy exits
- opposite-side entry treated as a reversal
- explicit close-then-open reversal sequencing
- end-of-data liquidation
- deterministic engine and evidence fingerprints

### 3.2 Side-aware execution
Entry and exit fills use adverse slippage by side:
- long entry: reference + slippage
- short entry: reference - slippage
- long exit: reference - slippage
- short exit: reference + slippage

### 3.3 Intrabar policy
OHLCV ambiguity remains explicitly policy-resolved. Default lifecycle policy is `ConservativeStopFirst`.

The implementation does not claim tick-level event ordering from OHLCV.

### 3.4 Daily risk boundary
The existing account ledger remains authoritative for simulated entry permission. If closing a losing trade reaches the daily loss lock, the subsequent reversal entry is rejected by the ledger rather than silently opened.

### 3.5 Signed ledger compatibility
The existing `AccountLedger` supports signed quantities. v20.24 exercises that capability for short-side research without creating a second ledger implementation.

## 4. Engineering decisions
1. **Close then open for reversals.** A reversal is not represented as an ambiguous quantity change. It has a separately evidenced exit and entry.
2. **Protective exits precede strategy intent.** If a stop/target/trailing barrier is triggered on a bar, that protective lifecycle event is resolved before a new strategy entry/reversal request.
3. **No automatic broker abstraction.** The lifecycle runner terminates at deterministic research fills and ledger accounting.
4. **Legacy compatibility retained.** Existing long-only `StrategyObservation` implementations are adapted into `PositionIntent` rather than rewritten in this build.
5. **Native runtime claims remain conservative.** No .NET SDK/compiler is available in the current environment, so source/structural validation is reported separately from native compilation/device validation.

## 5. Validation
Source-level validation:
- 66 C# files inspected
- structural brace balance: PASS
- new lifecycle source present: PASS
- architecture lifecycle checks present: PASS
- forbidden broker/live authority terms absent from lifecycle implementation: PASS
- signed long/short intent semantics: PASS
- side-aware entry/exit arithmetic: PASS
- conservative short-side ambiguity precedence: PASS
- signed short ledger open/close path represented in architecture tests: PASS

Native validation:
- .NET SDK/compiler: unavailable (`dotnet` not installed)
- Android SDK/device: not available
- Windows native runtime: not available

Therefore this build does **not** claim native compilation, Android installation, Windows execution, or device testing.

## 6. Safety and authority boundary
No live order capability was added. No broker connector was added. No application-setting mutation was added. No arbitrary imported-source execution was added. AI remains non-authoritative. The lifecycle runner is research-only.

## 7. Data fidelity
Current execution lifecycle can operate on canonical OHLCV. OHLCV does not establish exact intrabar sequencing. The declared ambiguity policy is recorded rather than presented as observed market behavior.

Tick, bid/ask, spread, replay, and measured latency fidelity remain dependent on corresponding source data.

## 8. Deferred User Contributions
Nothing is requested at this stage.

When the relevant workflow reaches native-data validation, use these short codes:
- **NT8 REPLAY** — native Market Replay sample
- **NT8 TICK** — NT8 tick export
- **NT8 MINUTE** — NT8 minute export
- **NT8 DAILY** — NT8 daily export
- **REPLAY MATCH** — paired replay/export validation sample
- **LATENCY DATA** — measured network/provider observations

These are intentionally deferred until the build reaches the corresponding workflow stage.

## 9. Next engineering phase — v20.25
Focus: **Account-Ledger Session Integrity + Trade Evidence Hardening**.

Planned work:
- explicit session-boundary accounting tests
- deterministic trade IDs that remain unique for same-millisecond events
- stronger position/equity event chain
- ledger evidence records for every entry/exit
- explicit daily-lock transition events
- reversal evidence linking exit and new entry
- durable evidence attachment to research jobs/checkpoints
- prepare multi-script/multi-instrument lifecycle isolation

The visual layer remains frozen while this core work proceeds.
