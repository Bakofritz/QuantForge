# QuantForge Futures Research Platform — Comprehensive Build Summary v20.17

## Release focus
Risk semantics, persistent risk-profile storage, instrument profiles, and execution-cost sensitivity. Basic grouped MAUI UI remains intentionally functional rather than visually refined.

## Implemented
- Structured canonical stop semantics with fixed-point default.
- Structured canonical trailing semantics with activation and trail distances.
- Structured fixed-contract position sizing with per-trade risk budget.
- Canonical risk/execution fingerprint binding.
- MES primary instrument profile and MNQ planned profile.
- Durable SQLite risk-settings persistence with fingerprint verification.
- Slippage/latency sensitivity matrix covering desktop Ethernet, desktop Wi-Fi, Android Wi-Fi, mobile data, and stress scenarios.
- Evidence-ready scenario fingerprints and friction as percentage of initial account.
- Existing $3,000 / one-contract / conservative-stop defaults retained.
- Grouped UI updated for v20.17 and core testing.

## Default risk profile
- Account: $3,000.
- Primary: MES.
- Planned: MNQ.
- Maximum: 1 contract.
- Initial stop: 6 points.
- Trailing activation: 4 points.
- Trailing distance: 2 points.
- Daily loss lock: $60.
- Commission assumption: $0.95/side, user-specified simulation assumption.
- Slippage: 1 tick/side.
- Base latency scenarios: 35 ms Ethernet, 80 ms desktop Wi-Fi, 80 ms Android Wi-Fi, 180 ms Android mobile data.

## Safety/data boundaries
Risk semantics are research-side only. No broker connection or live order authority was added. Latency is a scenario assumption, not a measurement. OHLCV cannot establish network or exchange execution latency.

## Validation
Source-level structural validation performed for all C# files; risk semantics, persistence schema, sensitivity matrix, instrument profiles, fingerprint binding, UI grouping, and live-order prohibition checks passed. Native .NET/MAUI/device compilation remains unavailable in the current environment and is not claimed.

## Next build
v20.18: connect canonical risk semantics to the event-driven fill ledger, add explicit profit-target semantics, position-size risk enforcement, and attach sensitivity results to durable research evidence.
