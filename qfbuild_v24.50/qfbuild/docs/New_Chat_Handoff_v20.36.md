# QuantForge v20.36 — New Chat Handoff

Continue from v20.36. The build now has terminal-state agreement and a controlled-concurrency design review layer.

Current authoritative constraints:
- Parallel execution is disabled.
- Live trading/order authority is absent.
- Recovery requires checkpoint and fingerprint validation plus a fresh lease.
- Immutable receipts preserve execution history; batch accounting counts one terminal outcome per case.
- Visual refinement remains deferred while core functionality is prioritized.

Next planned build: v20.37 — Concurrency Safety Gate + Resource/Storage Contention Design, without enabling actual parallel execution until native runtime validation is available.

Required build artifacts remain: User Manual, Comprehensive Build Summary, New-Chat Handoff, Release Manifest, Validation Result, and Source Hash manifest.
