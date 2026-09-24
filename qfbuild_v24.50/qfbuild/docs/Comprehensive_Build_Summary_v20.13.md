# QuantForge v20.13 — Comprehensive Build Summary

## Release
- Product: QuantForge Futures Research Platform
- Release: **v20.13**
- Baseline: **v20.12 Persistent Strategy Governance**
- Focus: **Canonical Strategy → Research Adapter + Deterministic Indicator/Signal Evaluation**
- Visual refinement: frozen by user preference; core functionality prioritized.
- PDF analysis: deferred.

## Objective
Turn the persisted, governed `CanonicalStrategyModel` into a deterministic research plan without executing or interpreting imported source code.

## Implemented
1. `CanonicalStrategyAdapter` converts only recognized canonical semantics into a bounded research plan.
2. EMA crossover semantics are supported when the canonical model contains EMA-family indicators and a cross signal.
3. Source-derived values are explicitly distinguished from adapter defaults. The current canonical model does not yet persist EMA periods, so the adapter uses declared defaults (9/21) and records that they are **not source-derived**.
4. Unsupported canonical semantics are reported explicitly rather than silently substituted.
5. Deterministic EMA indicator evaluation produces reproducible series fingerprints.
6. Deterministic EMA cross evaluation produces timestamped chart markers suitable for future chart rendering.
7. Chronology is enforced; the adapter rejects non-chronological bars.
8. Fidelity remains explicitly OHLCV bar-driven.
9. No source execution, live deployment, application mutation, or trading authority was added.

## Engineering decision
The adapter deliberately refuses to pretend that the v20.12 generic feature inventory contains semantic details that it does not actually contain. In particular, detection of `EMA/SMA indicator` does not prove a particular period, and `Cross signal` does not prove exact source-side entry/exit semantics. Therefore the current adapter labels its 9/21 periods as adapter defaults and blocks unsupported/ambiguous cases rather than manufacturing source fidelity.

## Validation
- 40 C# files structurally balanced.
- Canonical plan contract: PASS.
- EMA adapter: PASS.
- Cross signal adapter: PASS.
- Explicit unsupported reporting: PASS.
- No source execution: PASS.
- Chronology gate: PASS.
- Live authority absent: PASS.
- Deterministic fingerprinting: PASS.
- Indicator evaluation: PASS.
- Chart marker generation: PASS.
- Native .NET compilation: **not validated; `dotnet` unavailable in build environment**.
- Android/Windows runtime/device validation: not claimed.

## Known limitations
- Native C# compilation remains blocked by unavailable .NET SDK.
- Canonical strategy semantics are still a normalized feature-level representation rather than a full language AST/IR.
- EMA periods are not yet persisted as source-derived canonical parameters.
- Exact order semantics, stop/target semantics, position sizing, and multi-timeframe source semantics require richer canonical fields before they can be executed as source-faithful research.
- SMA execution is detected but not implemented by the current deterministic execution engine.
- No automatic live deployment authority exists.

## Next build
1. Extend canonical strategy schema with structured indicator parameters and explicit entry/exit/risk semantics.
2. Add canonical parameter persistence and immutable parameter fingerprints.
3. Add SMA/ATR evaluation and normalized indicator series.
4. Connect canonical research plans to the event-driven engine while preserving causal MTF constraints.
5. Generate indicator artifacts and strategy/backtest artifacts from the same canonical model.
6. Add multi-script/multi-instrument isolation at the canonical-model layer.
