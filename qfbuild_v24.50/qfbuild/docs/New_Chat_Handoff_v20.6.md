# QuantForge Futures Research Platform — New Chat Handoff v20.6

Continue the Master Build from v20.6 without asking the user to restate the architecture or authorization.

## Current state
- Native target: C# / .NET 10 LTS + .NET MAUI for Windows and Android.
- HTML radial UI is preserved as a reference/transitional artifact. Visual refinement is frozen until core functionality is fully runnable and validated.
- v20.5 established canonical data validation, causal multi-timeframe contracts, and native runtime orchestration.
- v20.6 adds deterministic grid optimization and chronological walk-forward analysis.

## v20.6 key components
- `QuantForge.Backtesting/OptimizationContracts.cs`
- `QuantForge.Backtesting/DeterministicOptimizer.cs`
- `QuantForge.Backtesting/WalkForwardAnalyzer.cs`
- `scripts/validate-v20.6.py`

## Research semantics
Optimization cases are isolated and deterministic. Walk-forward parameter selection occurs only on training windows, followed by evaluation on the subsequent test windows.

Current execution fidelity is OHLCV bar-driven only.

## Known blocker
The current environment does not provide the native .NET/Android/Windows build toolchain. Source-level validation therefore substitutes for native compilation/device validation and must not be represented as equivalent.

## Next build
Implement robustness and sensitivity analysis around optimization/walk-forward, then Monte Carlo/bootstrap analysis, persistent checkpointed optimization jobs, multi-script/multi-instrument isolation, and the canonical strategy/indicator model.

## Standing safety rules
No live trading, live orders, unrestricted AI mutation, canonical data mutation, undeclared code execution, or silent policy/manifest changes. Research AI remains read-only/non-authoritative.

## Mandatory release artifacts
Every build must include an updated detailed User Manual, standalone handoff, comprehensive build summary, validation record, release metadata, source hashes, limitations, and next engineering steps.
