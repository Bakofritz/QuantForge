# QuantForge Master Build v21.15 — Consolidated Strategy Semantics & Live-Trading Security Framework

## Consolidation
This milestone consolidates the compatible v21.11–v21.15 strategy-lifecycle/security work into one build effort.

## Strategy lifecycle continuation
The v21.10 provenance, capability-manifest, canonicalization, and research-registry boundaries remain the required upstream path.

## Live-trading framework
A secured live-trading pathway is now represented without enabling live trading by default. The pathway requires:
1. explicit administrative enablement;
2. fresh login reauthentication;
3. a fresh device-bound authenticator factor;
4. a fresh independent second factor;
5. explicit communication-scope configuration;
6. strategy, risk-policy, and runtime-qualification bindings;
7. an explicit arm action;
8. an expiring armed session;
9. auditable security events;
10. emergency disarm.

## Live communications
Users can explicitly control broker connection, live market data, order routing, account state, position updates, execution reports, heartbeat, risk alerts, audit telemetry, and notifications. Dependency rules constrain invalid combinations. Order routing requires broker connectivity and risk alerts. Position updates require account state.

## Bot arming
Trade-level bot integration has a dedicated final gate. A bot requires an armed live session, enabled order routing, healthy risk controls, explicit send-order capability, and retained emergency-flatten capability.

## Visual safety
The MAUI main page contains a deliberately hidden-by-default, high-contrast live-trading banner intended to become visible whenever the live state is active. The banner explicitly warns that real broker communications and orders may be active.

## Important boundary
This build establishes the framework and gates. It does not connect to a broker, submit real orders, store authentication secrets, or claim native runtime qualification. Credentials/factor secrets are intentionally kept outside these contracts.
