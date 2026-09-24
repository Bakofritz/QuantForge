# QuantForge New-Chat Handoff — v20.22

## Current baseline
**v20.23 — Canonical Position Intent + Side-Aware Fill Semantics**

Continue the Master Build autonomously under the standing directive. Core functionality remains the priority; visual refinement is frozen for later.

## Current architecture
`Canonical Data Gate → Causal Event Layer → Strategy/Indicator Observation → Risk/Execution Model → Ledger → Evidence`

Supporting systems include durable research jobs/checkpoints, optimization, walk-forward, robustness, Monte Carlo/bootstrap, strategy quarantine/governance, and NT8 export triangulation.

## v20.23 additions
- Canonical signed position intent contract.
- Side-aware strategy contract with legacy observation adapter.
- Adverse slippage arithmetic for long and short entry/exit.
- Side-aware OHLCV stop/target/trailing resolver.
- Architecture checks for signed position semantics.
- Preserved v20.22 ledger-bound execution and telemetry work.

## Known blockers
- No .NET SDK/compiler in the current environment.
- No Android SDK/device validation environment.
- No Windows native runtime validation environment.
- No native NT8 replay sample yet; proprietary native formats remain untrusted.

## Next engineering stage
Bind canonical long/short position intent into the ledger-bound runner with explicit reversal lifecycle and exactly-one-exit semantics. Then proceed to native NT8 sample forensics when a real sample becomes available.

## Deferred User Contributions
Do not request early:
- **NT8 REPLAY** — native replay sample.
- **NT8 TICK** — matching tick export.
- **NT8 MINUTE** — long-range minute export.
- **NT8 DAILY** — long-range daily export.
- **REPLAY MATCH** — paired replay/export sample.
- **LATENCY DATA** — measured network observations.

## Mandatory build outputs
Every release must include an updated User Manual, New-Chat Handoff, Comprehensive Build Summary, release metadata, validation results, limitations/fidelity boundaries, source hashes, and exact artifact hash/path.
