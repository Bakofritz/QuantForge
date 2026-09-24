# QuantForge New-Chat Handoff — v20.39

Continue the QuantForge Master Build from v20.39.

Current architecture milestone:
- Multi-case research is isolated by context/job/dataset/strategy/configuration fingerprints.
- Workers have explicit identity.
- Cases use durable leases and heartbeats.
- Checkpoints are fingerprint-bound and now immutable/idempotent by job+sequence.
- Execution receipts are durable and immutable.
- Recovery and continuation are checkpoint-bound.
- Batch accounting derives final case outcomes from durable receipts.
- Batch manifests and terminal-state agreement are validated.
- Concurrency safety gates and a dependency-light concurrency harness exist.
- v20.39 adds a storage-level concurrency adapter boundary and durable validation-result ingestion.
- Parallel research execution remains disabled.

v20.39 key artifacts:
- `IConcurrencyStorageValidationAdapter`
- `SqliteConcurrencyValidationAdapter`
- `IConcurrencyValidationResultStore`
- `concurrency_validation_results` SQLite table
- `ResearchRuntime.ValidateStorageConcurrencyAsync(...)`
- immutable checkpoint conflict protection

Native .NET/Android/Windows runtime validation remains unavailable in the current environment. Do not claim native execution merely because the adapter exists.

Next build: v20.40 — Native Validation Result Reconciliation + Storage Fault Injection Boundary.

Every build must continue to include User Manual, Comprehensive Build Summary, New-Chat Handoff, Release Manifest, Validation Result, and Source Hash manifest.
