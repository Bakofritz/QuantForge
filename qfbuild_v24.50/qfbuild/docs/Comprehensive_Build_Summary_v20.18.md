# QuantForge Futures Research Platform — Comprehensive Build Summary v20.18

## Build identity
- Release: v20.18
- Baseline: v20.17
- Focus: explicit profit-target semantics, risk-budget enforcement, event-driven risk binding, durable evidence-linked execution sensitivity, grouped functional UI.
- Native target: C# / .NET 10 LTS + .NET MAUI for Windows and Android.

## Implemented
1. Added `CanonicalProfitTargetKind` and `CanonicalProfitTargetSemantics`.
2. Added `ProfitTargetPoints` to governed risk settings. Default is `0`, meaning no target is silently invented.
3. Added deterministic profit-target evaluation to `RiskManagedExecutionModel`.
4. Added `RiskBudgetValidator` for pre-entry quantity and stop-risk checks.
5. Event-driven entry path now checks the governed risk budget before simulated entry.
6. Added `ResearchRuntime.RunExecutionSensitivityAsync` with canonical data gate, dataset identity validation, job lease, evidence hash, and append-only completion event.
7. Execution sensitivity remains an assumption matrix; latency is not fabricated from OHLCV.
8. Updated grouped MAUI UI to v20.18 and exposes the explicit target/risk state.

## Default risk profile preserved
- Initial account: $3,000
- Primary instrument: MES
- Planned instrument: MNQ
- Initial maximum: 1 contract
- Initial stop: 6 points
- Trailing activation: 4 points
- Trailing distance: 2 points
- Profit target: disabled by default (0 points)
- Maximum stop-risk budget: $30 per trade
- Daily loss lock: $60
- Simulation commission assumption: $0.95 per side
- Slippage: 1 tick per side
- Default scenario: Windows Wi-Fi, 80 ms estimated round trip

The $30 risk budget applies to modeled price-loss exposure at the configured stop. Commission/slippage are reported separately as execution friction and are not silently counted as guaranteed price-loss risk.

## Execution path
`Causal market event → strategy observation → risk-budget validation → simulated entry → modeled slippage → position state → profit target / initial stop / trailing stop → simulated exit → evidence`

No broker connection, live order, or source-code execution was added.

## Data fidelity
Historical OHLCV remains OHLCV. Network latency, broker routing, exchange matching, packet loss, and intrabar sequencing are not inferred from bars. Sensitivity scenarios are explicitly labeled engineering assumptions.

## Validation status
Source-level validation is required for this release. Native compilation/device validation remains blocked by the unavailable .NET/MAUI/Android/Windows toolchain in the build environment and must be performed before production/runtime claims.

## Known limitations
- Long-only event-driven risk path is the current supported execution subset.
- Profit target is fixed-points only; ticks/percentage targets are represented by canonical enums but not yet translated by the execution model.
- Short-side risk execution is not yet enabled.
- Intrabar ordering between stop and target on OHLCV bars is inherently ambiguous and is not resolved without a higher-fidelity event stream.
- Sensitivity reports use scenario assumptions, not measured network telemetry.
- Native MAUI runtime/device validation is pending.
- Visual fidelity work remains intentionally deferred.

## Next engineering steps
1. Explicit target/stop intrabar resolution policy for bar data, with conservative ambiguity handling.
2. Short-side canonical execution semantics.
3. Instrument-specific execution profiles for MNQ.
4. Persist sensitivity reports as durable research-progress/evidence artifacts.
5. Connect risk-managed execution to full ledger/account-equity accounting.
6. Add measured latency telemetry adapter without granting trading authority.
7. Native compile, Windows runtime, Android runtime, and device validation when toolchain is available.

## Safety boundary
Imported scripts remain quarantined/audited before canonicalization. No imported source is executed. AI remains analysis/proposal-only. Live trading and live orders remain disabled.
