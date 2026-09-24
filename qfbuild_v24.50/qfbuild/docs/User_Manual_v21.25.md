# QuantForge v21.25 User Manual

## 1. What changed
v21.25 adds the security control plane that sits between ordinary research and any future live-trading integration. The default is still research-only.

## 2. Live Trading Security Control Plane
Open **Settings & Security → Live Trading Security**.

The page distinguishes:
- **Research Only** — no live broker communications.
- **Pathway Enabled / Not Armed** — the secure pathway is available, but no live session exists.
- **Live Armed** — a short-lived, explicitly authorized session exists and high-contrast warnings must be shown.
- **Emergency Disarmed / Session Expired** — live order authority is removed until the full authorization flow is repeated.

## 3. Changing security settings
A privileged security-setting change requires:
1. Fresh login reauthentication.
2. Device-bound authenticator verification.
3. Independent second-factor verification.

The framework does not store the user's authentication secrets. Platform-specific credential handling belongs to the future Android/.NET authentication adapter.

## 4. Communication controls
Live communications are individually scoped. Examples include broker connection, live market data, account state, position updates, execution reports, order routing, risk alerts, heartbeat, audit telemetry, and user notifications.

Dependencies are enforced. For example, order routing cannot be enabled without broker connection and risk alerts.

## 5. Bot arming
Registering a bot does not grant order authority. A bot must explicitly declare `SendOrders` and retain `EmergencyFlatten` before the final live-order gate can admit it.

## 6. Final live-order gate
A future live order must satisfy all of these conditions:
- armed live session is valid and unexpired;
- security settings are current;
- approved strategy binding matches;
- active risk policy matches risk-health binding;
- risk heartbeat is healthy;
- broker connection is healthy and advertises order submission;
- order-routing and broker communications are enabled;
- recovery/reconciliation state is clear;
- bot explicitly allows order submission;
- emergency flatten capability remains available.

Any failure is fail-closed.

## 7. Persistence
Security settings may be stored encrypted using `EncryptedLiveSecuritySettingsStore`. The application must obtain the encryption key from an appropriate platform secure-key facility; the framework intentionally does not persist that key in the settings file.

## 8. Current limitation
This build provides the security architecture and admission framework only. It does not connect to a broker or place live orders.
