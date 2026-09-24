# QuantForge Master Build — New Chat Handoff v21.25

Current consolidated baseline: **v21.25**.

Previous baseline: v21.15 Strategy Semantics + Live Trading Security Framework.

v21.25 adds:
- encrypted persistent live-security settings;
- device-bound and independent-second-factor authentication adapter interfaces;
- broker capability discovery;
- live risk heartbeat/health monitoring;
- final live-order admission gate;
- dedicated Live Trading Security Control Plane UI;
- architecture checks for all of the above.

Important safety boundary: no real broker order submission exists in this build. The default authority policy remains blocked unless a future integration deliberately supplies an explicit live-enabled authority policy. Do not bypass that boundary.

Recommended next consolidated block:
1. platform-native Android authentication adapters (WebAuthn/passkey/device credential and independent factor provider);
2. secure key-store adapter for encrypted settings;
3. persistent audit sink with tamper-evident chaining;
4. broker connector interface with capability handshake and connection lifecycle;
5. risk engine health contract and automatic emergency disarm on heartbeat loss;
6. live communication dependency graph and user-facing settings bindings;
7. pre-order preview/confirmation record before any future broker submission adapter;
8. deterministic fault-injection tests across all final admission gates.

Keep research/read-only execution isolated from live trading. Keep imported script execution quarantined and governed through the existing scrub/lifecycle pipeline.
