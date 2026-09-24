# QuantForge User Manual v20.16

## Purpose
This release makes the initial risk and execution assumptions visible and grouped in the native UI while continuing the research-only safety boundary.

## Default research profile
**Account:** $3,000
**Primary:** MES
**Planned:** MNQ
**Maximum initial position:** 1 contract
**Initial stop:** 6.00 points
**Trailing:** activates after 4.00 points; 2.00-point trail
**Daily loss lock:** $60
**Commission assumption:** $0.95 per side (user-specified simulation assumption)
**Slippage:** 1 tick per side
**Default connection scenario:** Windows Wi-Fi, 80 ms estimated round trip

## Connection scenarios
- Windows Ethernet: 35 ms estimate
- Windows Wi-Fi: 80 ms estimate
- Android Wi-Fi: 80 ms estimate
- Android mobile data: 180 ms estimate
These values are scenario estimates only. They must not be interpreted as measured broker or exchange latency.

## How risk is modeled
The research execution model applies explicit slippage to simulated fills and binds the selected risk profile to the canonical execution-plan fingerprint. The risk manager can trigger an initial stop or trailing stop in the causal event-driven research path.

## Commission note
The $0.95/side value is retained because it was requested as the initial simulation assumption. Current NinjaTrader public pricing checked for this build lists $0.39 per side for Micro contracts on the Free plan. Exchange, clearing and NFA fees are additional. Always verify the actual account agreement before using live-cost assumptions.

## UI groups
The home screen is intentionally organized into functional groups rather than receiving another visual redesign: Project & Governance, Research Controls, Account/Risk/Execution Defaults, Connection Scenarios, System Status and Next Research Targets.

## Safety
Imported scripts remain quarantined and non-executable. AI remains non-authoritative. Live trading remains disabled.
