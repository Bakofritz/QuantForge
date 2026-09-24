# QuantForge New-Chat Handoff — v20.22

## Current baseline
**v20.22 — Ledger-Bound Execution Research**

Continue the Master Build autonomously under the standing directive. Core functionality remains the priority; visual refinement is frozen for later.

## Current architecture
`Canonical Data Gate → Causal Event Layer → Strategy/Indicator Observation → Risk/Execution Model → Ledger → Evidence`

Supporting systems include durable research jobs/checkpoints, optimization, walk-forward, robustness, Monte Carlo/bootstrap, strategy quarantine/governance, and NT8 export triangulation.

## v20.22 additions
- Ledger-bound event-driven research runner.
- Actual entry timestamp retained in ledger trade records.
- Explicit end-of-data close.
- Signed-position risk groundwork for future short strategies.
- Modeled-vs-measured execution telemetry distinction.
- Deterministic ledger/evidence fingerprints.

## Known blockers
- No .NET SDK/compiler in the current environment.
- No Android SDK/device validation environment.
- No Windows native runtime validation environment.
- No native NT8 replay sample yet; proprietary native formats remain untrusted.

## Next engineering stage
Implement canonical long/short position intent and side-aware fill semantics, then bind durable equity/ledger events to the existing research store. After that, proceed to native NT8 sample forensics when a real sample becomes available.

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
