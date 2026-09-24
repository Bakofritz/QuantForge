# QuantForge v22.30 — Detailed User Manual

## What changed
v22.30 makes the live environment easier to understand and safer to qualify. The system now has an explicit snapshot describing what the live environment can currently observe and whether order-routing gates are actually clear.

## Simple live-environment summary
- Research/backtesting remains separate from live execution.
- Live market-data observation can exist without live order authority.
- Broker authentication is a distinct state and must be bound to the current authority fingerprint.
- An unresolved execution recovery state blocks live routing.
- An unhealthy audit chain blocks live recovery.
- An unhealthy risk state blocks live routing.
- The live pathway must be explicitly armed before order-routing capability can become available.
- Reconnection/authentication does not automatically arm the live pathway.
- An external broker order ID must be bound to the internal execution identity before a definitive broker-side outcome can be trusted.
- Production live order submission is not implemented by this build.

## State model
The live environment reports one primary state: PlatformNotReady, PlatformReady, BrokerDisconnected, BrokerAuthenticated, RecoveryRequired, RiskBlocked, LivePathwayDisarmed, LivePathwayArmed, OrderRoutingAvailable, or OrderRoutingBlocked.

The capability snapshot separately reports read-market-data, account-state, positions, execution-report, order-routing, cancellation, and emergency-flatten capabilities.

## Operational guidance
1. Start the application and allow startup integrity checks to complete.
2. Verify the audit chain is healthy.
3. Authenticate to the supported broker adapter when one is installed.
4. Confirm the broker session identity is bound to the current authorization context.
5. Resolve every unknown execution before attempting recovery completion.
6. Confirm risk health.
7. Keep the live pathway disarmed unless live operation is intentionally required.
8. Treat the capability snapshot as authoritative for what the current runtime is permitted to expose.

## Fail-closed behavior
Unavailable native security, broker authentication, audit verification, identity binding, or recovery state does not silently downgrade to software fallbacks or blind retries.
