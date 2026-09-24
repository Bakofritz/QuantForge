# QuantForge User Manual — v20.14

## What v20.14 adds
v20.14 gives governed strategy models a structured semantic representation and persists that representation. This lets QuantForge derive an indicator artifact and a deterministic research/backtest artifact from the same semantic definition.

## Governed workflow
1. Acquire source.
2. Quarantine it.
3. Run static audit.
4. Review detected components.
5. Explicitly select components.
6. Commit a canonical research model.
7. Build canonical semantics.
8. Review source-derived versus adapter-default parameters.
9. Generate indicator/research artifacts.
10. Run deterministic research only when the semantics are supported.

## Parameter provenance
Every structured parameter records whether it came from the source model. Adapter defaults are not presented as source facts. For the current EMA crossover adapter, 9/21 are defaults when the source model identifies EMA/crossover behavior without numeric periods.

## Indicator artifact
The indicator artifact contains deterministic EMA series and cross markers. Markers are research/visual artifacts. They do not place orders or grant execution authority.

## Backtest artifact
The canonical backtest bridge uses the existing deterministic EMA crossover engine. It requires supported semantics and preserves the existing OHLCV bar-driven fidelity declaration.

## Unsupported semantics
If the canonical model contains semantics that the current engine cannot faithfully execute, QuantForge reports them and blocks that adapter path rather than silently substituting another rule.

## Persistence
Canonical semantics are stored against both the canonical model fingerprint and semantic fingerprint. A strategy identity cannot silently be rebound to a different semantic representation.

## Safety
Imported source remains inert. No source compilation or execution occurs. No live trading authority is enabled. AI and research layers remain non-authoritative.

## Data fidelity
OHLCV remains OHLCV. The system does not infer tick sequence, bid/ask, spread, replay, or intrabar execution fidelity from bar data.

## Current validation limitation
The current environment does not contain the .NET SDK/compiler or Android/Windows SDKs. Source-level validation is therefore separate from native compilation and device validation.

## Deferred work
Visual refinement is intentionally deferred while core runtime functionality is built. PDF analysis is also deferred.

## v20.15 additions
- SMA and ATR can be evaluated as deterministic research indicators.
- SMA crossover is explicitly indicator-only until an execution adapter supports it.
- Canonical timeframe bindings reject unavailable declared timeframes.
- Causal observations use the existing point-in-time aggregation layer.
