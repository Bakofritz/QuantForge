# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.27

## Build identity
- Release: v20.27
- Baseline: v20.26 Multi-Script / Multi-Instrument Research Isolation
- Theme: Multi-Case Execution Coordinator
- Native target: C# / .NET 10 LTS + .NET MAUI
- Visual refinement: intentionally deferred

## Objective
Turn the v20.26 isolation contract into a deterministic multi-case execution boundary without merging mutable strategy, position, ledger, or evidence state.

## Implemented
1. `MultiCaseExecutionRequest` defines batch identity, device owner, lease duration, maximum case count, and immutable research descriptors.
2. `MultiCaseItemResult` records per-context completion, failure, cancellation, and result fingerprint.
3. `MultiCaseExecutionResult` records the aggregate batch fingerprint and cancellation state while retaining per-case lineage.
4. `MultiCaseExecutionCoordinator` validates the batch through `IsolatedResearchCoordinator` before execution.
5. Each case acquires its own execution lease using its own JobId.
6. Optional durable `IResearchJobStore` integration records queued/running/completed/failed/canceled job states.
7. Each case executes against its own `ResearchContextState` and returns an independent result fingerprint.
8. Aggregate fingerprinting combines batch identity and per-case outcomes without merging mutable state.
9. Cancellation stops the coordinator at a case boundary and releases the active lease safely.
10. Failures are recorded per case rather than converting the entire batch into a false successful result.
11. A configurable `MaxCases` guard prevents an unbounded batch from being submitted to the coordinator.
12. Architecture checks exercise two independent cases, per-case execution, aggregate fingerprint generation, and completed-state reporting.

## Architectural reasoning
v20.26 established state isolation; v20.27 adds orchestration without prematurely claiming parallelism. Execution is intentionally sequential in this release. This reduces concurrency complexity until native runtime/resource measurements are available and keeps lease, cancellation, checkpoint, and evidence behavior deterministic.

The coordinator is deliberately callback-based: the existing governed research engines remain responsible for their own canonical-data gates, checkpoints, detailed evidence, and domain-specific execution. The coordinator owns case scheduling and lifecycle, not strategy semantics.

Per-case leases are tied to the case JobId rather than a single batch lease. This prevents one batch from accidentally becoming a single mutable execution authority and leaves room for future safe concurrency.

## Safety boundary
- No live trading.
- No broker routing.
- No arbitrary imported-source execution.
- No AI application mutation authority.
- No canonical market-data mutation.
- No automatic parallel execution claim.

## Validation
- C# source structure checked across the complete source tree.
- Multi-case architecture checks added.
- Batch maximum guard checked.
- Independent case execution checked.
- Per-case completion state checked.
- Aggregate SHA-256 fingerprint checked.
- Native compilation not performed because the environment has no .NET SDK/compiler.
- Android/Windows device/runtime validation not performed.

## Fidelity boundary
Scheduling and isolation do not increase market-data fidelity. OHLCV remains OHLCV. Tick, bid/ask, and Market Replay fidelity require their actual validated event sources.

## Deferred User Contributions
No contribution is needed for v20.27.

Future codes:
- NT8 REPLAY — native Market Replay sample
- NT8 TICK — NT8 tick export
- NT8 MINUTE — NT8 minute export
- NT8 DAILY — NT8 daily export
- REPLAY MATCH — paired replay/export validation
- LATENCY DATA — measured network observations

## Next build
v20.28 — Durable Multi-Case Checkpoint Orchestration: persist coordinator cursor, per-case lifecycle/checkpoint identity, safe resume after interruption, and batch-level evidence without duplicating completed cases.
