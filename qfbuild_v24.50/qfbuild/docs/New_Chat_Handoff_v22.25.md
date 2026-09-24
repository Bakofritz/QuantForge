# New-Chat Handoff — QuantForge v22.25

Continue from v22.25.

### Current architecture
- Governed strategy import/audit/quarantine and research registration.
- Read-only multi-script research optimization.
- Persistent live-security settings and two-factor step-up boundary.
- Tamper-evident audit concepts and startup integrity checks.
- Secure-key runtime boundary with fail-closed unavailable implementation.
- Broker order-state normalization and unknown-order protection.
- Recovery/reconciliation orchestration with authority revalidation.
- v22.25 adds live-environment state/capability snapshot, recovery authorization binding, and adversarial recovery matrix.

### Next logical work
- Controlled platform-specific secure-key adapters for Android Keystore and Windows protected storage.
- Native authenticator/passkey boundary.
- Broker-specific authenticated session and external-order-ID binding.
- Transactional audit-chain startup verification.
- End-to-end adversarial submit→timeout→disconnect→restart→unknown→reconcile→revalidate tests.

### Permanent safety rules
- Live pathway OFF by default.
- No automatic retry of unknown broker outcomes.
- Reconnect does not re-arm.
- Research authority never implies live order authority.
- No claim of production broker execution without controlled integration validation.
