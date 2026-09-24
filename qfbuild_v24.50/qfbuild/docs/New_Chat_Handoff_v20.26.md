# QuantForge — New-Chat Handoff v20.26

## Current release
**v20.26 — Multi-Script / Multi-Instrument Research Isolation**

Baseline: v20.25.

## Completed
- Explicit research context identity.
- Deterministic context fingerprinting.
- Independent mutable research state.
- Batch context creation.
- Duplicate context rejection.
- Runtime exposure through `ResearchRuntime.CreateIsolatedResearchCases`.
- Architecture checks proving state does not leak between distinct cases.

## Architecture rule
Immutable datasets may be shared by fingerprint. Mutable strategy, position, ledger, and evidence-reference state must be owned by the individual research context.

## Current limitation
v20.26 establishes the isolation contract and state boundary. It does not claim native parallel execution or distributed scheduling. The current environment still lacks the .NET SDK/compiler and Android/Windows runtime validation.

## Safety
Research-only. No live orders, broker routing, arbitrary source execution, unrestricted AI mutation, or canonical-data mutation.

## Next phase
v20.27 — Multi-Case Execution Coordinator with deterministic scheduling, independent leases/checkpoints/cancellation, and aggregate evidence that preserves per-case lineage.

## Deferred contributions
NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, REPLAY MATCH, and LATENCY DATA remain deferred until their appropriate workflow stages.

Continue the Master Build from v20.26 without restating the architecture.
