# QuantForge New-Chat Handoff — v20.38

Continue the QuantForge Master Continuous Build Directive from v20.38.

Current architecture includes governed research cases, worker identity, leases/heartbeats, durable checkpoints, recovery continuation, immutable execution receipts, batch accounting, batch manifests, terminal-state agreement, and a concurrency safety gate.

v20.38 adds a dependency-light concurrency validation harness covering:
1. lease acquisition races
2. idempotent receipt append
3. contradictory receipt conflict rejection
4. checkpoint monotonicity
5. cancellation race
6. deterministic aggregate ordering

Parallel execution remains OFF. Do not claim native runtime validation. The environment lacks the .NET SDK/Android/Windows native toolchains.

Next planned phase: v20.39 native/SQLite contention adapter boundary and validation-result ingestion, still gated from enabling parallel execution.

Visual refinement remains deferred; prioritize core functionality.

Every future build must include User Manual, Comprehensive Build Summary, New-Chat Handoff, Release Manifest, Validation Result, and Source Hash manifest.

User contributions remain deferred until needed; use established code words when a contribution becomes relevant.
