# QuantForge — New-Chat Handoff v20.8

## Current release
**v20.8 — Deterministic Monte Carlo / Bootstrap Research**

Baseline: v20.7

## Completed
1. Deterministic EMA backtest exposes completed-trade PnL observations.
2. Added deterministic bootstrap-with-replacement analyzer.
3. Added seed and report fingerprints.
4. Added percentile, mean, median, drawdown, and non-positive-PnL sample statistics.
5. Added native runtime `RunMonteCarloAsync` path.
6. Monte Carlo is behind canonical data validation and dataset identity checks.
7. Job lease, durable job/checkpoint, evidence, and append-only completion event are retained.
8. Safety and OHLCV fidelity boundaries remain explicit.
9. Updated manual, summary, manifest, validation, and source hash inventory.

## Important interpretation rule
Bootstrap probabilities describe the generated resampling experiment. They are not probabilities of future market performance.

## Current fidelity
Imported research datasets remain OHLCV. No tick, bid/ask, spread, or exact intrabar sequencing is claimed.

## Current limitation
Monte Carlo is checkpoint-attached but the iteration loop is not yet independently resumable at an iteration cursor.

## Next phase: v20.9
- persistent iteration/case cursors;
- cancellation-aware chunks;
- lease-expiry recovery;
- fingerprint-validated resume;
- duplicate-result/evidence prevention;
- multi-script/multi-instrument isolation;
- crash/fault injection.

Continue the Master Build without restating the architecture. Visual refinement remains frozen until core runtime capability is substantially complete.
