# QuantForge New-Chat Handoff — v20.44

Continue the Master Build from v20.44.

## Current state
- Native architecture: C# / .NET 10 LTS / .NET MAUI target.
- Local-first SQLite evidence architecture.
- Multi-script/multi-instrument isolated cases.
- Sequential multi-case execution; parallel execution disabled.
- Worker/lease/checkpoint/receipt/recovery architecture hardened through v20.43.
- v20.44 adds an explicit transactional terminal commit boundary.

## v20.44 terminal commit
SQLite now atomically commits terminal job state, execution receipt, evidence, and terminal event in one transaction, conditional on an unexpired lease owned by the expected worker.

## Important validation limitation
`dotnet` is unavailable in the current environment. Do not claim native compilation or runtime execution. Source-level validation is allowed and must be labeled as such.

## Next
v20.45 should build the native transaction/fault execution harness if runtime becomes available. If not, continue source-level provider-neutral terminal commit contracts and deterministic fault scenarios.

## Safety
No live trading, no live orders, no unrestricted AI mutation, no canonical data mutation, no undeclared execution. Visual refinement remains deferred.

## Deferred User Contributions
Do not request NT8/data contributions until the relevant build stage. Use the established short code words when needed: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, NT8 CONFIG.
