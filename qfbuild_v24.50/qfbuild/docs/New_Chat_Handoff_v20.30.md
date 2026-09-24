# QuantForge New-Chat Handoff — v20.30

Current release: v20.30.

Continue the Master Build without re-planning the architecture. v20.30 hardened resource governance with durable execution receipts, explicit worker identity, batch resource accounting, batch timeout enforcement, and receipt identity stabilization.

## Next phase
v20.31 — Durable Worker/Lease Reconciliation + Recovery Audit.

Priority:
1. reconcile durable receipts against lease history;
2. detect stale worker ownership and orphaned cases;
3. record recovery/reclaim evidence;
4. prevent duplicate completion after worker replacement;
5. bind batch accounting to checkpoint/recovery state;
6. preserve sequential execution until native runtime validation supports safe concurrency testing.

Do not resume visual refinement; core functionality remains the priority.

## Validation limitation
The environment still lacks the .NET SDK/Android/Windows native toolchains, so source-level and deterministic structural validation are the highest claims available.

## Deferred User Contributions
- NT8 REPLAY / NT8 TICK / NT8 MINUTE / NT8 DAILY: later native-source validation.
- MES DATA / MNQ DATA / ROLL DATA: later instrument/rollover fidelity validation.
- STRATEGY SRC / STRATEGY TEST: later governed strategy acquisition validation.
- REPLAY MATCH: later cross-source replay validation.
- LATENCY DATA / NT8 CONFIG: later measured execution/environment validation.

Workflow: attach the relevant material later using its code word; it is not required for v20.30.
