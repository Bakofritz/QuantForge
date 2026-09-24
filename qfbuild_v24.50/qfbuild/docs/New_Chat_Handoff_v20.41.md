# QuantForge New-Chat Handoff — v20.41

Current release: **20.41 — Test-Only Storage Fault Seam + Native SQLite Fault Execution**.

Continue from the v20.40 storage fault boundary. The current source adds an internal fault-injector seam to `SqliteLocalStore` and a real-SQLite validation adapter. Seven storage points are covered: lease acquire, lease renew, checkpoint write, receipt write, evidence write, validation-result write, and job-state write.

`ResearchRuntime.ValidateNativeStorageFaultBoundaryAsync(databasePath)` runs the native adapter and records its fingerprint as evidence/events.

Important: the environment used to create this release does not contain the .NET SDK/native runtime, so native execution of the adapter is not claimed. Parallel research execution remains disabled.

Next build: deterministic native fault-result reconciliation and failure-recovery invariants. Verify no false-success terminal state after injected failures and keep retry/recovery fingerprint-bound.

Required per-build artifacts remain: User Manual, Comprehensive Build Summary, New-Chat Handoff, Release Manifest, Validation Result, Source Hashes.

Deferred user contributions remain deferred until their relevant validation stage; use the established contribution code words rather than requesting data prematurely.
