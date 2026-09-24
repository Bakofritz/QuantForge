# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.5

## 1. Build identity
- Release: v20.5
- Baseline: v20.4
- Focus: canonical data gating + causal multi-timeframe research boundary
- Native target: C# / .NET 10 LTS + .NET MAUI, Windows + Android
- Visual state: frozen by directive; no visual refinement added.

## 2. Engineering work
### Architecture dependency correction
The prior project graph had Core referencing Backtesting while Backtesting referenced Core contracts, creating a circular project dependency. `ResearchRuntime` was moved to a dedicated `QuantForge.Runtime` project. Core now owns contracts; Backtesting consumes Core contracts; Runtime orchestrates Core/Data/Backtesting/Storage.

### Canonical data gate
`CanonicalDataGate` now blocks native research execution when the dataset is empty, missing identity, contains invalid OHLC relationships, violates strict chronology, or lacks OHLCV fidelity. Limited execution fidelity is recorded as a warning rather than silently upgraded.

### Causal multi-timeframe layer
`CausalTimeframeAggregator` creates point-in-time views from bars available at the current event timestamp. `CausalBarView.IsComplete` distinguishes a completed bucket from a forming bucket. `StrategyContext.Timeframes` exposes these views to strategies.

### Event ordering
The event-driven engine no longer sorts its input internally. It rejects non-causal timestamps so malformed or future-reordered input cannot be silently normalized into a plausible result.

## 3. Design tradeoffs
- The gate is deliberately strict on structural data integrity and deliberately non-judgmental about fidelity: it records what the source actually contains.
- Multi-timeframe aggregation is causal but not tick-accurate. With OHLCV input, intrabar ordering remains unresolved.
- The runtime project adds one layer to the solution, but removes a circular dependency and establishes a cleaner orchestration boundary for optimization, recovery, and device coordination.

## 4. Validation
Static/source validation verifies:
- no Core → Backtesting project reference;
- Backtesting → Core reference exists;
- Runtime project references Core/Data/Backtesting/Storage;
- App references Runtime;
- canonical gate and error codes exist;
- causal timeframe contracts exist;
- strategy context carries timeframe views;
- event engine rejects non-causal ordering;
- v20.5 manual, handoff, manifest, and validation record exist.

Native compile/device validation remains unavailable because the environment lacks the .NET SDK/compiler and Android/Windows SDKs.

## 5. Safety
No live trading, live orders, arbitrary code execution, canonical-data mutation, unrestricted AI mutation, or manifest bypass was introduced.

## 6. Data fidelity
OHLCV only remains the current imported-data boundary. No bid/ask, tick sequencing, spread, or replay fidelity is claimed.

## 7. Next phase
1. Move optimization/walk-forward/robustness onto durable job/checkpoint infrastructure.
2. Add explicit OHLCV execution models and ambiguity policies.
3. Native script scrubber/quarantine.
4. Canonical strategy/indicator model with source lineage.
5. Native .NET/MAUI compile and runtime validation when toolchain is available.
