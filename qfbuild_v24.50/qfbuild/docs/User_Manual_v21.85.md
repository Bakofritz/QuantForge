# QuantForge v21.85 — Detailed User Manual

## 1. Default Safety State
QuantForge continues to start with live trading disabled. Nothing in this milestone changes that default.

## 2. Platform Authentication
The application expects a platform-backed secure-key provider for production authentication. If none is configured, authentication-dependent broker preparation fails closed.

## 3. Broker Connectivity
Broker connectivity is represented independently from live execution authority. A connection may be technically available while live order admission remains denied.

## 4. Reconnection
Transient broker degradation may be eligible for bounded reconnection attempts. Reconnection never creates permission to submit an order and never retries an ambiguous order automatically.

## 5. Audit Persistence
Live-security events can be persisted transactionally in SQLite. The store contains audit metadata only and must not be used as a secret store.

## 6. Ambiguous Orders
If a broker outcome is unknown, do not manually resubmit the same logical order. QuantForge's execution architecture requires reconciliation by idempotency key before another submission can be admitted.

## 7. Production Platform Setup
Native Android Keystore, Windows secure storage, passkey/authenticator, and broker-specific authentication must be supplied through their designated adapters. The test-only ephemeral key provider is not suitable for production.

## 8. Safety Principle
A functioning broker transport does not mean that live orders are authorized. The final live-order admission gate remains the controlling authority boundary.
