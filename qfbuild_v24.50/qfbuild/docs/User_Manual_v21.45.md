# QuantForge v21.45 — Detailed User Manual

## Research Mode
Continue using QuantForge normally for importing, auditing, canonicalizing, backtesting, optimization, and AI-assisted read-only research. Nothing in this milestone enables live trading by itself.

## Live Security Flow
When live trading is eventually configured, the intended sequence is:

1. Log in normally.
2. Complete the required fresh reauthentication and two independent step-up factors.
3. Enable the live pathway only through the protected security settings.
4. Configure the permitted communication capabilities.
5. Establish a healthy broker capability handshake.
6. Verify risk-health monitoring is healthy.
7. Arm the governed live session.
8. Prepare the exact order preview.
9. Review the preview and explicitly confirm it.
10. The system binds the confirmation to the preview and security state.
11. The final admission gate runs again.
12. A future broker adapter may receive the explicitly authorized handoff.

## What a User Should Expect
Changing a security setting, broker capability, strategy binding, risk binding, runtime qualification, or other protected state can invalidate an outstanding confirmation. The user must then generate and review a new preview and confirmation.

A confirmation cannot be reused. Once consumed, the token is permanently invalid for another handoff.

## Audit Chain
The persistent audit sink is intended to preserve a tamper-evident sequence of security events. If the chain fails validation at startup, QuantForge must treat the live-security audit state as unsafe and block live execution until the problem is resolved through an authorized recovery procedure.

## Important Current Limitation
v21.45 does **not** send orders to a live broker. The final authority boundary intentionally ends before broker submission.
