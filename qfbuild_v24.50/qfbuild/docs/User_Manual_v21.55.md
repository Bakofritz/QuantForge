# QuantForge v21.55 — Detailed User Manual

## What changed
This release adds the safety infrastructure immediately before real broker execution. It does not activate live trading by itself.

## Normal live-order flow
1. Enable the live pathway through the protected security settings.
2. Complete the required fresh-login and independent step-up authentication controls.
3. Configure only the communication capabilities required by the operation.
4. Arm the authorized session.
5. Pass strategy, runtime, risk, recovery, broker capability, and bot admission checks.
6. Review the exact order preview.
7. Confirm the order.
8. QuantForge rechecks admission and the security binding.
9. A unique execution intent is persisted before broker submission.
10. The broker adapter returns a definitive result or an unknown result.
11. Unknown results are reconciled by querying the broker; they are never automatically retried.

## Duplicate protection
The execution idempotency key is deterministically bound to the preview and security binding. An existing intent blocks another submission for the same exact execution context.

## Ambiguous outcomes
If the application loses contact after a submission or the broker gives a non-definitive response, the intent becomes `Unknown`. The system must reconcile it before another submission is allowed.

## Audit chain
The tamper-evident audit chain is checked at the execution boundary. An invalid chain blocks execution. Execution preparation and outcome transitions are recorded.

## Current limitation
No production broker adapter is included in this release. Native platform authentication and secure key storage remain adapter boundaries to be integrated with platform-specific implementations.
