# QuantForge User Manual v20.18

## Purpose
v20.18 adds governed profit-target semantics, pre-entry risk-budget enforcement, event-driven risk binding, and evidence-linked execution sensitivity research.

## Default research profile
- Account: $3,000
- Instrument: MES
- Position limit: 1 contract
- Initial stop: 6 points
- Trailing: activates after 4 points, trails by 2 points
- Profit target: disabled unless explicitly configured
- Stop-risk budget: $30/trade
- Daily loss lock: $60
- Commission assumption: $0.95/side
- Slippage: 1 tick/side
- Default network scenario: Windows Wi-Fi, estimated 80 ms round trip

## How risk is applied
A simulated entry is permitted only when quantity is within the governed contract limit and the initial stop-risk exposure does not exceed the configured maximum risk per trade. Modeled commission and slippage remain separately reported execution friction.

## Profit target
A fixed-point profit target may be configured explicitly. The default is disabled. The system does not infer a target from an imported strategy when the source does not contain one.

## Execution sensitivity
The sensitivity analyzer can compare controlled scenarios for latency and slippage across Windows Ethernet, Windows Wi-Fi, Android Wi-Fi, Android mobile data, and stress scenarios. These are estimates for research sensitivity, not measured network or exchange latency.

## Data fidelity
OHLCV data cannot establish tick sequencing, bid/ask, spread, broker routing, packet loss, or actual mobile/desktop network latency. Higher-fidelity execution requires corresponding event data or measured telemetry.

## Safety
- Imported source is never executed by the scrubber/governance layer.
- Live orders are disabled.
- AI has no live-order authority.
- Canonical market data is not silently mutated.

## UI
The MAUI testing surface is grouped into Project & Governance, Research Controls, Account/Risk/Execution, Connection Scenarios, and System Status. This is a functional test surface, not the final visual design.
