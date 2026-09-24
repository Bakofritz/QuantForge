# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.28

## Build identity
- Release: v20.28
- Baseline: v20.27
- Focus: durable multi-case checkpoint orchestration
- Native target: C# / .NET 10 LTS + .NET MAUI, Windows + Android
- Visual state: frozen/deferred by directive

## Implemented
1. Added durable batch progress using the existing ResearchProgress/ResearchCheckpoint infrastructure.
2. Bound persisted batch state to a deterministic multi-case fingerprint.
3. Added progress-hash integrity verification before recovery.
4. Added completed/failed/canceled case skipping so completed cases are not silently rerun.
5. Persisted case results after each case.
6. Persisted cancellation/recovery state conservatively.
7. Added duplicate-completion protection through persisted terminal case state.
8. Integrated the coordinator into ResearchRuntime.
9. Preserved one independent execution lease per research case.

## Engineering rationale
The coordinator now has a durable cursor without introducing a second persistence system. The existing research-progress and checkpoint contracts already provide fingerprint-bound resumability, so v20.28 extends those primitives to the batch layer. Mutable strategy, position, ledger, and evidence state remains case-local.

A resumed batch first validates the batch fingerprint and progress hash. Terminal cases are retained and skipped. Non-terminal work can resume from the first unresolved case. This is deliberately conservative: no claim of transparent mid-case continuation is made; an interrupted case must rely on its own case-level checkpoint infrastructure.

## Validation
Source-level structural validation passed for all C# files. Native .NET/MAUI, Android, and Windows runtime validation remains blocked because the required SDK toolchains are unavailable in the current environment. Parallel execution remains intentionally deferred.

## Safety
No live trading, order routing, arbitrary imported-source execution, canonical-data mutation, or unrestricted AI application mutation was introduced.

## Data fidelity
No change. Current native research fidelity remains whatever the source actually provides; OHLCV is not upgraded to tick/bid/ask/replay fidelity.

## Next phase
v20.29 should harden resource governance around multi-case execution: explicit per-batch/per-case resource budgets, lease heartbeat coordination, cancellation polling, and deterministic execution receipts before any parallel scheduling is enabled.

## Deferred User Contributions
No contribution is required at this stage. NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, and NT8 CONFIG remain deferred until the relevant validation stage.
