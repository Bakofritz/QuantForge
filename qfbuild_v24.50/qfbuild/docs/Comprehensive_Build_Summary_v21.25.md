# QuantForge v21.25 — Consolidated Live Security Persistence, Authentication, Broker Capability, Risk Health, and Final Order Admission

## Purpose
This build consolidates the next compatible live-trading security block after v21.15. It establishes the persistent and runtime framework required before any future broker order adapter may be permitted to submit real orders.

## Included capabilities
- Encrypted-at-rest persistent live-security settings with no authentication secrets stored by the framework.
- Research-only defaults remain mandatory: live pathway disabled and all live communication capabilities denied until explicitly authorized.
- Platform authentication adapter boundary for a device-bound authenticator and an independent second factor.
- Short-lived authentication challenges and factor proofs suitable for future Android/.NET platform implementations.
- Broker capability discovery abstraction and capability fingerprinting.
- Live risk-health monitor with heartbeat freshness enforcement.
- Final live-order admission gate combining session, strategy, risk, runtime, broker, communications, recovery, bot, and emergency-control requirements.
- Dedicated Live Trading Security Control Plane UI with high-contrast warning states.
- Architecture tests for persistence defaults, independent authentication factors, broker capability gating, risk heartbeat gating, recovery blocking, and emergency-control retention.

## Security invariants
1. Enabling the pathway is not equivalent to arming a session.
2. Fresh login reauthentication plus two independent step-up factors remain required for privileged live-security changes.
3. Order routing requires broker connectivity and risk-alert communications.
4. A live bot must explicitly declare order submission and retain emergency flatten capability.
5. Broker order-submission capability must be discovered rather than assumed.
6. Risk health must be current; stale heartbeat blocks admission.
7. Unresolved recovery/reconciliation state blocks admission.
8. Strategy, risk, and runtime fingerprints must remain bound to the admitted operation.
9. Security settings are persisted encrypted, while the encryption key remains an external platform responsibility.
10. No code in this build submits real broker orders.

## Explicit non-claims
The environment does not contain the .NET SDK, Android SDK, SQLite runtime, platform authenticator APIs, or broker credentials/connectors needed for native integration testing. Accordingly, this package does not claim native compilation, device authentication verification, SQLite execution, broker connectivity, or live-order execution.

## Relationship to prior governance
Imported strategies remain subject to the v21.05 quarantine/audit/scrub flow and v21.10 lifecycle governance. Research authority remains distinct from live-trading authority. AI remains outside the final live-order admission authority path.
