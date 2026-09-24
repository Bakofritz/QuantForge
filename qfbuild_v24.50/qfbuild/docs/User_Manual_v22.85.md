# QuantForge v22.85 — Detailed User Manual

## Purpose
This release strengthens startup, secure-runtime, session-renewal, audit-continuity, and recovery-state controls without granting new implicit live authority.

## Safe Operating Model
1. Import strategies through quarantine and static audit.
2. Use research/backtesting independently of live execution.
3. Treat read-only market/account observation as non-order authority.
4. Require current authentication, session identity, audit continuity, risk health, authority validity, and recovery clearance before live continuation.
5. If any required condition is missing, the system fails closed.
6. Unknown broker outcomes require reconciliation; do not blind-retry.

## Live-Environment State & Capabilities
- Live routing is disabled unless every required gate is satisfied.
- Secure-runtime lifecycle state can invalidate continued use.
- Session renewal is bound to the prior lease and current authority fingerprint.
- Audit checkpoints must match the verified chain head and remain fresh.
- Persisted recovery state is restorable only while fresh and complete.
- Startup/recovery replay is blocked by unresolved unknown executions or any failed health/authority gate.
