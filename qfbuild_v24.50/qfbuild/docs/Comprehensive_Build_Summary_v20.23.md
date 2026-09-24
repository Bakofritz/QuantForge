# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.23

## 1. Build identity
- Release: **v20.23 — Canonical Position Intent + Side-Aware Fill Semantics**
- Baseline: v20.22
- Product: QuantForge Futures Research Platform
- Core priority: runtime architecture, data integrity, governance, backtesting, storage, synchronization, AI/Scout safety.
- Visual refinement: intentionally deferred.

## 2. Engineering objective
v20.22 connected simulated execution accounting to the account ledger. v20.23 removes the remaining ambiguity between long-only legacy strategy observations and the future canonical position model by introducing explicit signed position intent and side-aware fill arithmetic.

The intended execution boundary is now:

`Market Event → Strategy Observation → Position Intent → Risk Gate → Side-Aware Fill → Position → Exit Policy → Ledger → Evidence`

The entire path remains research-only.

## 3. Implemented
### Canonical Position Intent
Added `PositionIntentKind`:
- Hold
- EnterLong
- EnterShort
- Exit
- ReverseToLong
- ReverseToShort

`PositionIntent` carries a signed quantity and reason without exposing broker authority.

### Side-aware strategy contract
Added `ISideAwareStrategy`, which extends the existing strategy contract and can return a canonical `PositionIntent` using the current signed position as context.

Legacy `StrategyObservation` remains supported through `StrategyObservationAdapter` so existing long-only research does not need to be rewritten immediately.

### Side-aware fill model
Added `SideAwareFillModel`.
- Long entry: adverse slippage moves the simulated fill upward.
- Short entry: adverse slippage moves the simulated fill downward.
- Long exit: adverse slippage moves the simulated fill downward.
- Short exit: adverse slippage moves the simulated fill upward.

This prevents a sign error from making short-side simulation artificially favorable.

### Side-aware OHLCV exit resolver
Added `SideAwareIntrabarResolver` for long and short positions. It delegates barrier ambiguity to the existing declared OHLCV policy and does not claim to reconstruct intrabar tick order.

### Architecture validation
Added `PositionIntentArchitectureChecks` covering:
- signed long/short intent mapping;
- hold neutrality;
- adverse entry slippage by side;
- adverse exit slippage by side;
- legacy observation adaptation.

## 4. Engineering decisions / tradeoffs
1. **Keep legacy strategy compatibility.** Existing `EnterLong/ExitLong` strategies remain usable while new strategies can adopt signed intent.
2. **Do not rewrite the account ledger yet.** It already supports signed positions; v20.23 supplies the canonical contract needed to exercise that capability safely.
3. **Do not infer intrabar order.** OHLCV still cannot establish whether a target or stop occurred first when both barriers are inside one bar. The configured ambiguity policy remains authoritative for simulation.
4. **Keep broker authority absent.** Position intent is a research contract, not an order instruction to a broker.
5. **Delay reversal implementation in the runner.** The contract supports reversals, but a dedicated reversal lifecycle will be bound only after entry/exit side semantics are fully validated, avoiding implicit double-fills.

## 5. Validation
Source-level validation performed:
- C# source inventory and structural brace checks.
- Position-intent contract presence.
- Legacy adapter presence.
- Side-aware fill polarity checks.
- Side-aware resolver presence.
- Prohibited-authority scan.

Native compilation/device validation remains unavailable because the current environment does not contain the required .NET/MAUI, Android, or Windows SDK runtime toolchain.

Therefore this release does **not** claim native compile, Android runtime, Windows runtime, or device validation.

## 6. Safety / authority boundary
No live trading authority was introduced.
No broker order-routing API was introduced.
No application-setting mutation authority was introduced.
No canonical market-data mutation authority was introduced.
No arbitrary source execution was introduced.

## 7. Data-fidelity boundary
Current research data remains declared OHLCV unless a higher-fidelity event source is actually supplied and validated. Side-aware fill arithmetic is a deterministic simulation assumption; it is not observed exchange/broker execution.

## 8. Deferred User Contributions
Nothing is required from the user at this stage.

When the corresponding stages are reached:
- **NT8 REPLAY** — small native Market Replay sample; validates native replay identification/normalization.
- **NT8 TICK** — matching exported tick sample; triangulates native replay against explicit event data.
- **NT8 MINUTE** — long-range minute export; validates continuity and aggregation.
- **NT8 DAILY** — long-range daily export; validates decade-scale history efficiently.
- **REPLAY MATCH** — paired replay/export package for direct comparison.
- **LATENCY DATA** — measured connection observations to replace modeled latency assumptions where appropriate.

## 9. Next engineering phase
v20.24 should bind canonical position intent into the ledger-bound runner with explicit:
- long/short entry;
- long/short exit;
- exactly-one-exit-per-position-per-event;
- reversal lifecycle;
- side-aware slippage;
- stop/target/trailing policies;
- daily loss lock;
- deterministic evidence events.

After that, proceed to the NT8 native sample-forensics stage when an actual sample becomes available.
