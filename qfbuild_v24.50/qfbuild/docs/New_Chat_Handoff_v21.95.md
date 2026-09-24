# QuantForge v21.95 — New Chat Handoff

Continue the QuantForge Master Build from **v21.95**.

## Current State
v21.95 consolidates:
- native Android/Windows authentication and secure-key adapter boundaries;
- deterministic canonical live-order normalization and fingerprinting;
- bounded broker recovery coordination;
- disconnected/degraded transport blocking;
- Unknown execution-intent blocking;
- and targeted architecture validation.

## Existing Security Architecture That Must Be Preserved
- Strategy import quarantine and feature scrubbing.
- Research-only capability separation.
- Live pathway disabled by default.
- Fresh-login reauthentication plus two independent step-up factors.
- Explicit live communication scope.
- Risk-health heartbeat and emergency disarm.
- Broker capability discovery.
- Tamper-evident/transactional audit boundaries.
- Human order confirmation.
- Final admission recheck.
- Durable execution intent and idempotency.
- Ambiguous outcome → reconciliation, never blind retry.
- Broker transport capability separate from execution authority.

## Suggested v22.05+ Direction
Consolidate the next production-integration block around:
1. Real Android Keystore adapter implemented only in the Android application target.
2. Real Windows secure-key adapter implemented only in the Windows application target.
3. Native passkey/authenticator adapter integration.
4. Broker-specific authenticated connector contracts.
5. Broker response/state normalization and external-order identity binding.
6. Transactional audit-chain integration with startup verification.
7. End-to-end disconnect/reconnect/recovery orchestration.
8. Exact confirmation-to-broker payload fingerprint verification.
9. Comprehensive fault-injection tests across timeout, disconnect, duplicate, restart, unknown outcome, stale confirmation, and risk-heartbeat loss.
10. Keep actual broker order submission behind a separately explicit implementation and authorization milestone.

## Important Constraint
Do not represent platform adapter boundaries as production implementations until they are compiled and tested in their actual native targets. Do not submit real broker orders merely because the transport interface exists.
