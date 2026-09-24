# QuantForge New-Chat Handoff — v20.13

## Current release
**v20.13 — Canonical Strategy Research Adapter**

Baseline: v20.12.

## Completed
- Added `CanonicalStrategyAdapter` in `QuantForge.Backtesting`.
- Added canonical research-plan contracts.
- Added deterministic EMA evaluation.
- Added deterministic EMA crossover signal evaluation and chart markers.
- Added explicit unsupported-feature reporting.
- Preserved chronology/canonical-data assumptions.
- Preserved no-source-execution and no-live-deployment boundaries.
- Preserved v20.12 persistent quarantine, audit, selection, canonical-model, and lineage storage.

## Critical semantic limitation
The v20.12 canonical model records feature names but does not yet persist exact indicator periods or complete source-level rule semantics. v20.13 therefore refuses to claim source fidelity where the canonical model lacks the required information. EMA 9/21 are adapter defaults and are explicitly marked non-source-derived.

## Next build
1. Structured canonical indicator parameters.
2. Structured entry/exit/risk semantics.
3. Immutable parameter fingerprints.
4. SMA/ATR evaluation.
5. Canonical plan → event-driven backtest adapter with causal MTF enforcement.
6. Shared canonical source for generated indicator and strategy artifacts.
7. Multi-script/multi-instrument isolation.

## Validation limitation
`dotnet` is unavailable in the current environment. Structural/source validation passes; native compilation and Android/Windows device validation are not claimed.

## Permanent build rules
Every release must include an updated User Manual, standalone handoff, build summary, release metadata, validation results, fidelity boundaries, safety boundaries, known limitations, next steps, and exact artifact/hash information. Visual refinement remains deferred while core functionality is prioritized.
