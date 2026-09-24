# QuantForge — New-Chat Handoff v20.27

## Current release
**v20.27 — Multi-Case Execution Coordinator**

Baseline: v20.26.

## Completed
- Multi-case request and result contracts.
- Deterministic batch aggregation.
- Per-case execution leases.
- Optional durable job lifecycle integration.
- Per-case completion/failure/cancellation reporting.
- Maximum-case resource guard.
- Aggregate evidence fingerprint without merging mutable case state.
- Architecture checks for two independent research cases.

## Architecture rule
Every research case retains its own strategy, position, ledger, checkpoint, and evidence lineage. The coordinator may aggregate immutable result identities, but must never merge mutable execution state.

## Concurrency rule
v20.27 intentionally executes cases sequentially. It establishes safe case orchestration before parallel scheduling. Do not describe this release as parallel or distributed execution.

## Native validation limitation
The current environment still lacks the .NET SDK/compiler, Android SDK/device, and Windows native runtime. Source-level validation is reported separately from native runtime validation.

## Safety
Research-only. No live orders, broker routing, arbitrary source execution, unrestricted AI mutation, or canonical-data mutation.

## Next phase
v20.28 — Durable Multi-Case Checkpoint Orchestration with coordinator cursor, per-case resume identity, interruption recovery, and duplicate-completion protection.

## Deferred contributions
NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, REPLAY MATCH, and LATENCY DATA remain deferred until their appropriate workflow stages.

Continue the Master Build from v20.27 without restating the architecture.
