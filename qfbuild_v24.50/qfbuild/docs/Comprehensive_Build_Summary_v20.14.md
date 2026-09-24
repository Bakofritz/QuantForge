# QuantForge v20.14 — Comprehensive Build Summary

## Release
- Product: QuantForge Futures Research Platform
- Release: **v20.14**
- Baseline: **v20.13 Canonical Strategy Research Adapter**
- Focus: **Structured Canonical Strategy Semantics + Persistent Semantic Lineage + Shared Indicator/Backtest Artifacts**
- Visual refinement: frozen; core functionality remains the priority.
- PDF analysis: deferred.

## Objective
Move beyond generic feature strings into a structured canonical representation that can preserve parameter provenance and explicit entry/exit/risk semantics. The same semantic representation now feeds both indicator artifacts and deterministic backtest execution.

## Implemented
1. Added structured canonical parameter definitions with default/minimum/maximum/step, provenance, source-derived flag, and immutable fingerprint.
2. Added structured indicator semantics including indicator kind, price source, parameters, provenance, and fingerprint.
3. Added structured entry semantics including direction, trigger, required indicators, timeframe declaration, provenance, and fingerprint.
4. Added structured exit semantics and risk semantics with explicit unsupported-state reporting.
5. Added `CanonicalStrategySemanticsAdapter` to translate only recognized canonical model semantics; raw source remains inert.
6. Added persistent SQLite storage for canonical semantic representations, bound to the canonical model fingerprint and semantic fingerprint.
7. Added canonical-model-to-backtest bridge using the existing deterministic EMA engine.
8. Added canonical-model-to-indicator artifact generation using the same semantic definition and signal evaluation path.
9. Preserved deterministic chart signal markers and research-only authority boundaries.
10. Preserved OHLCV fidelity declaration; no tick/bid/ask/replay fidelity is inferred.

## Key engineering decision
The canonical model is still the source of truth. Indicator and strategy/backtest artifacts are derived from the same semantic fingerprint. This reduces the risk of indicator/strategy divergence caused by separate interpretations.

Parameter provenance is explicit. When a source only identifies an EMA family but does not provide a numeric period, v20.14 records 9/21 as adapter defaults with `SourceDerived=false`. The system does not represent those defaults as facts extracted from the imported source.

## Safety boundary
- Imported source is never compiled, loaded, or executed by this layer.
- Unsupported semantics are reported rather than silently approximated.
- Canonical research remains non-live.
- `LiveDeploymentEligible` remains false for the existing canonical model contract.
- No order, broker, application-setting, canonical-market-data, or undeclared-code authority was added.

## Data fidelity
Current native research remains OHLCV bar-driven. The build does not claim bid/ask, spread, tick ordering, Market Replay, or exact intrabar sequencing.

## Validation
- C# files structurally balanced: **42**.
- Structured parameter contract: PASS.
- Indicator semantics: PASS.
- Entry semantics: PASS.
- Exit semantics: PASS.
- Risk semantics: PASS.
- Immutable semantic fingerprints: PASS.
- Source provenance retained: PASS.
- Unsupported semantics explicit: PASS.
- Deterministic semantic adapter: PASS.
- Canonical backtest bridge: PASS.
- Canonical indicator artifact: PASS.
- Chart signal reuse: PASS.
- Semantic persistence contract: PASS.
- Semantic persistence schema: PASS.
- No source execution: PASS.
- No live authority: PASS.
- Chronology preserved: PASS.
- Native .NET compilation: **not validated; `dotnet` is unavailable in the build environment**.
- Android/Windows runtime/device validation: not claimed.

## Known limitations
- Full NinjaScript/Pine AST/IR extraction is not implemented.
- Source-derived numeric parameters are only available where the canonical source model already contains them; the current generic feature inventory still cannot recover all source semantics.
- Stop/target/position-sizing semantics that lack explicit structured values remain unsupported or informational.
- SMA/ATR execution paths are not yet connected to the deterministic execution engine.
- Multi-script and multi-instrument orchestration at the canonical-model layer remains future work.
- Native MAUI compilation/device validation remains blocked by the available toolchain.

## Next build
1. Add deterministic SMA and ATR indicator evaluators and normalize indicator-series contracts.
2. Add explicit numeric stop/target/position-sizing semantics and bounded execution-model translation.
3. Connect canonical execution plans to the causal event-driven engine for multi-timeframe research.
4. Add multi-script and multi-instrument isolation around canonical plans.
5. Attach canonical semantic fingerprints to durable research jobs/evidence.
6. Continue toward generated indicator and strategy artifacts without creating an automatic live-deployment path.
