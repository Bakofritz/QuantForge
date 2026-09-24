# QuantForge v20.30 — Durable Resource Receipts + Batch Accounting + Worker Identity

## Purpose
v20.30 hardens the v20.29 resource-governance layer by making execution receipts durable, binding receipts to an explicit worker identity, recording batch-level resource accounting, and distinguishing batch timeout from per-case timeout and user cancellation.

## Implemented
- Durable SQLite execution-receipt records with immutable receipt fingerprints.
- Durable batch resource-accounting snapshots.
- Explicit `ResearchWorkerIdentity` and identity fingerprint.
- Receipt fingerprints no longer depend on elapsed time or heartbeat count; runtime measurements remain evidence fields.
- Batch-duration enforcement through a linked batch cancellation token.
- Explicit timeout classification: `CASE_RESOURCE_TIMEOUT`, `BATCH_RESOURCE_TIMEOUT`, `CANCELED`.
- Aggregate elapsed milliseconds and heartbeat renewals across completed cases.
- Duplicate receipt protection and immutable conflict detection.
- Runtime injection point for a dedicated `IResourceReceiptStore`.
- Existing sequential concurrency gate remains enforced; parallel execution is not enabled merely by adding accounting.

## Engineering decisions
1. A receipt is execution evidence, not a deterministic performance result. Its identity fingerprint therefore excludes wall-clock measurements.
2. Worker identity is explicit and fingerprinted so future multi-device/multi-worker reconciliation can distinguish execution provenance.
3. Batch accounting is a mutable snapshot keyed by batch ID; individual receipts remain immutable.
4. Batch timeout is fail-closed and does not silently resume a partially executed case.
5. Parallel execution remains deferred until native runtime/resource validation is available.

## Validation
- 73 C# source files present.
- Brace/structural balance: PASS.
- v20.30 contract checks added for worker identity and durable receipt fields.
- Native .NET compilation/device execution remains unavailable in the current environment; no native runtime pass is claimed.

## Safety boundary
No live trading, order submission, unrestricted AI mutation, canonical-data mutation, or undeclared code execution was added.
