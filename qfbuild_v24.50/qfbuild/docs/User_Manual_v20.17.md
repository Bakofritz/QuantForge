# QuantForge User Manual v20.17

## Risk and execution profile
The default research profile starts at $3,000, uses MES as primary, limits initial size to one contract, and applies a 6-point initial stop with a 4-point trailing activation and 2-point trailing distance.

## Costs
The default simulation uses $0.95 commission per side and one tick slippage per side. These are configurable research assumptions.

## Connectivity scenarios
Use Windows Ethernet, Windows Wi-Fi, Android Wi-Fi, Android mobile data, and stress scenarios to compare modeled friction. Latency values are estimates and must not be interpreted as measured broker/exchange latency.

## Persistence
Risk profiles are stored locally in SQLite with a fingerprint. A mismatch prevents silent use of altered settings.

## Instrument scope
MES is the primary profile. MNQ is retained as planned until separately calibrated.

## Safety
Imported source is never executed by this layer. Risk modeling cannot place live orders.
