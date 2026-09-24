# QuantForge — New-Chat Handoff v20.5

## Current release
**v20.5 — Canonical Data Gate + Causal Multi-Timeframe Runtime**

Baseline: v20.4

## Completed
1. Broke the Core ↔ Backtesting circular project dependency by moving orchestration `ResearchRuntime` into `QuantForge.Runtime`.
2. Added mandatory `CanonicalDataGate` before the native research runner proceeds.
3. Added `CausalTimeframeAggregator` and point-in-time `CausalBarView` contracts.
4. Extended `StrategyContext` with synchronized timeframe views.
5. Updated event-driven execution to preserve source chronology instead of sorting events and to reject non-causal ordering.
6. Updated native engine lineage to `qf-native-v20.5`.
7. Preserved v20.4 durable job/checkpoint/lease infrastructure.

## Important architecture rule
The canonical data gate is upstream of research execution. A research path must not bypass it.

## Fidelity boundary
Current imported datasets are OHLCV. Multi-timeframe views are derived causally from those bars. This does not create tick, bid/ask, spread, or replay fidelity.

## Validation limitation
No .NET SDK/compiler, Android SDK, or Windows SDK is available in the current environment. Source/structural validation is therefore reported separately from native runtime validation.

## Next phase
- Attach optimization, walk-forward, and robustness jobs to durable checkpoints.
- Add declared OHLCV execution models and ambiguity policies.
- Port first-line script scrubber/quarantine into native services.
- Add strategy/indicator canonical model and governed source lineage.
- Validate the native solution with actual .NET/MAUI SDKs when available.

Continue the Master Build from v20.5 without restating the architecture.
