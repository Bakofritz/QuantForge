# QuantForge v21.95 — Detailed User Manual

## What changed
v21.95 strengthens the security boundary around platform authentication, broker-order identity, and broker recovery. It does not turn live trading on automatically.

## Live Trading Safety Model
The normal progression remains:
1. Start in research mode.
2. Enable the live pathway only through the protected security workflow.
3. Complete the required fresh-login and two-factor checks.
4. Verify risk health and broker capability state.
5. Review the exact order preview.
6. Confirm the order through the confirmation boundary.
7. QuantForge performs a final admission check.
8. An execution intent is persisted before broker submission.
9. The broker request uses the deterministic canonical order representation.
10. Any ambiguous broker outcome must be reconciled before another execution is allowed.

## Platform Authentication
Android and Windows now have explicit integration points for native authenticators and secure keys. In this build those boundaries intentionally report unavailable/pending until the native application layer supplies the real platform APIs.

Never replace an unavailable native provider with plaintext credential storage.

## Order Normalization
The canonical order representation normalizes symbol, side, order type, time-in-force, quantity, and optional price fields. It uses invariant formatting so equivalent orders produce the same canonical representation and fingerprint.

The fingerprint can be used to bind the order shown in a confirmation screen to the order later handed to a broker connector.

## Recovery
If the broker connection is disconnected or degraded, live execution remains blocked. If any prior execution has an ambiguous outcome, the recovery coordinator blocks new execution until reconciliation resolves that specific intent.

Reconnect attempts are bounded. Reconnecting does not automatically retry an order.

## Current Production Boundary
v21.95 is an architecture/integration milestone. It does not provide a production broker connector or submit real orders.

## Recommended Operator Practice
Before any future live integration is enabled, verify the account, strategy authorization, risk limits, broker capabilities, communication scope, order preview, and recovery state. Treat any Unknown execution state as requiring reconciliation rather than retrying.
