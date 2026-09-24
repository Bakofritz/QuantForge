# QuantForge v22.25 — Detailed User Manual

## What changed
The platform now exposes a simple, deterministic snapshot of the live environment. It separates what the application can observe from what it is authorized to do.

## Live environment state
Possible states: `Offline`, `PlatformReady`, `BrokerConnected`, `RecoveryRequired`, `Armed`, and `Blocked`.

## Capability meanings
- **Read-only market data:** observation only; no order authority.
- **Account/position observation:** observation only; no mutation authority.
- **Research/backtesting:** research authority remains independent of live trading.
- **Strategy audit/import:** imported code remains quarantined/governed before system access.
- **Live order routing:** appears available only when all final security, broker, risk, recovery, audit, and secure-runtime gates are satisfied.
- **Emergency disarm:** safety control remains available.

## Recovery behavior
If an order may have been submitted but its outcome is unknown, do not retry blindly. Preserve execution identity and require broker reconciliation. A restart with unresolved unknown execution remains `RecoveryRequired`. Any changed authority binding invalidates the previous recovery authorization.

## Operational checklist
1. Verify startup integrity.
2. Verify secure runtime availability.
3. Verify audit-chain health.
4. Verify broker connection and risk health.
5. Resolve any unknown executions.
6. Revalidate authority.
7. Review the live-environment capability summary.
8. Only then consider arming the live pathway.

## Important
v22.25 remains an architecture and validation build. Production broker submission is not enabled merely by installing this package.
