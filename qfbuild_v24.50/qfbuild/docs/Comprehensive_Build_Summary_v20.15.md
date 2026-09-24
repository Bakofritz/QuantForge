# QuantForge v20.15 — Comprehensive Build Summary

## Release focus
Structured indicator evaluation and causal multi-timeframe binding for the canonical strategy research layer.

## Baseline
v20.14 Canonical Semantics & Research Artifacts.

## Implemented
- Deterministic SMA evaluation over canonical market bars.
- Deterministic ATR evaluation using true-range calculations and simple rolling averaging.
- Canonical research-plan recognition of SMA crossover semantics as indicator-only unless an execution adapter explicitly supports SMA.
- Explicit separation between indicator capability and backtest execution capability.
- Canonical timeframe binding with explicit unavailable-timeframe rejection.
- Causal observation bridge through the existing CausalTimeframeAggregator.
- Deterministic fingerprints for indicator series, timeframe bindings, and causal observations.
- Existing EMA canonical backtest/indicator path retained.

## Engineering reasoning
The build expands the semantic vocabulary without allowing unsupported execution semantics to become silently executable. SMA can now be evaluated and visualized, but the existing EMA-only deterministic order simulation remains the execution boundary. ATR is available as an indicator calculation but is not automatically converted into a stop/target rule unless the canonical semantics explicitly provide the required risk semantics and a bounded execution adapter exists.

The timeframe bridge deliberately reuses the causal aggregation layer rather than creating a second MTF implementation. This preserves the existing no-lookahead boundary and reduces divergence between strategy research paths.

## Data fidelity
Current canonical research remains OHLCV bar-driven. No bid/ask, tick sequencing, replay, spread, or intrabar execution fidelity is inferred.

## Safety boundary
Imported source remains inert. No compile/load/invoke/execute path was added. No live order authority, broker authority, or application-setting mutation authority was added.

## Validation
Source-level validation passed for all C# files. Native .NET/MAUI compilation and Android/Windows device validation remain unavailable in the current environment and are not claimed.

## Known limitations
- SMA crossover execution is not yet connected to the deterministic order simulator.
- ATR is an indicator evaluator; semantic stop/target execution requires a separately governed execution adapter.
- Timeframe strings that are not explicitly supplied by the source remain unbound rather than invented.
- No native runtime/device validation in this environment.
- Visual refinement remains intentionally frozen.
- PDF analysis remains deferred.

## Next build
v20.16: governed execution semantics for explicit stop/target/risk rules, canonical position sizing, and a shared event-driven execution adapter, while preserving the no-live-trading boundary.
