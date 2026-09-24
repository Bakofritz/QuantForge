# QuantForge User Manual v21.15

## Live trading security
Live trading is disabled by default. To enable the pathway, an authorized administrator must use the security settings flow. A fresh login reauthentication and two independent step-up factors are required for security-setting changes and for arming.

### Step-up layers
- Layer 1: device-bound authenticator (for example a platform passkey/security key implementation at the UI/integration layer).
- Layer 2: independent second factor (for example an authenticator application or separate security key implementation at the UI/integration layer).

The framework stores only verification metadata/fingerprints, not factor secrets.

## Communication controls
Each live communication capability is independently represented. Options can be enabled only when required dependencies are satisfied. QuantForge fails closed when a required dependency disappears while armed.

## Live bot integration
A bot cannot arm simply because a strategy is registered. It must pass the live session, communication, risk-health, explicit order-capability, and emergency-control gates.

## Visual indication
When live trading is active, the application is designed to present a bold, high-contrast warning banner. Research-only mode remains the default visual state.

## Emergency disarm
Emergency disarm clears the armed live session. Communication changes that make the live configuration unsafe also force disarm.
