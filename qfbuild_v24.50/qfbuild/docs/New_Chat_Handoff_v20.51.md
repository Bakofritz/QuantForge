# QuantForge New-Chat Handoff — v20.51

Current release: v20.51
Focus: Recovery Preflight Binding Fault-Injection and Reconciliation.

The previous build v20.50 bound an approved recovery preflight to a specific checkpoint and worker lease and revalidated both immediately before continuation.

v20.51 adds a deterministic fault harness covering nine cases: checkpoint drift, lease drift, lease expiry, missing checkpoint, missing lease, wrong worker, binding persistence failure, identical binding retry, and conflicting binding.

All conflicting cases fail closed. An identical binding retry is idempotent.

Validation is source-level only because the environment does not provide the .NET SDK/runtime. No native SQLite execution claim is made.

Next build: v20.52 — durable recovery audit/reconciliation binding for these outcomes.

Deferred User Contributions: none needed now. NT8/data contribution codes remain deferred until the native data bridge phase.
