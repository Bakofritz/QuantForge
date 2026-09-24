# QuantForge Futures Research Platform — New-Chat Handoff v20.4

## Continue point
Continue the Master Build from v20.4 without asking the user to restate the architecture.

## Current release
- v20.4
- Baseline v20.3
- Core priority: runtime functionality, data integrity, governance, backtesting, storage, synchronization, recovery, AI/Scout safety.
- Visual refinement is intentionally deferred.

## Architecture
C# / .NET 10 LTS + .NET MAUI shared application core for Windows and Android. Local-first SQLite state. Separate execution leases protect mutable jobs. Immutable evidence/events are intended for future cross-device synchronization.

## v20.4 implemented
1. Durable ResearchJob lifecycle.
2. Durable checkpoints with dataset/configuration/engine fingerprints.
3. Persisted cancellation requests.
4. RecoveryRequired state after lease-expiry recovery conditions.
5. Event-driven MarketEvent → Strategy → SimulationOrder → ExecutionFill boundary.
6. EMA-cross adapter retained as a deterministic compatibility strategy.
7. Explicit research-only authority boundary.

## Conservative recovery rule
Never auto-resume an expired job merely because a checkpoint exists. Validate checkpoint fingerprints, acquire a fresh execution lease, then resume. Changed inputs must not silently reuse old checkpoint state.

## Validation state
Source-level validation completed for required v20.4 contracts and storage schema changes. Native compilation/device validation remains blocked because .NET/Android/Windows SDK tooling is unavailable in the current build environment.

## Next build
1. Mandatory canonical data gate upstream of all research paths.
2. Causal multi-timeframe event aggregation.
3. Explicit execution-model hierarchy.
4. Durable optimization/walk-forward/robustness scheduling.
5. Native script scrubber/quarantine service migration.
6. Native Windows/Android compile and runtime validation when SDK tooling is available.

## Safety
No live trading, live orders, arbitrary code execution, canonical-data mutation, manifest mutation, or unrestricted AI application mutation.

## Data fidelity
Current supplied native datasets are OHLCV. Do not claim bid/ask, tick sequencing, or replay fidelity without the corresponding source.

## Mandatory per-build artifacts
Updated User Manual, New-Chat Handoff, Comprehensive Build Summary, release manifest, validation record, and artifact hashes.
