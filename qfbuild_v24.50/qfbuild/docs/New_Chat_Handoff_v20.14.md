# QuantForge — New-Chat Handoff v20.14

## Current release
**v20.14 — Structured Canonical Strategy Semantics + Shared Research Artifacts**

Baseline: v20.13.

## Completed
- Structured canonical parameters with provenance and immutable fingerprints.
- Structured indicator, entry, exit, and risk semantics.
- Persistent canonical semantic storage in SQLite.
- Canonical semantic adapter with explicit unsupported-state handling.
- Canonical semantics → deterministic EMA backtest bridge.
- Canonical semantics → indicator artifact and chart-signal path.
- Shared semantic fingerprint prevents independent indicator/strategy definitions from silently diverging.
- No source execution and no live deployment authority.

## Important rule
Do not infer source-derived numeric parameters when the canonical model does not contain them. Adapter defaults must remain explicitly marked as defaults.

## Fidelity
Native research remains OHLCV bar-driven. Do not claim bid/ask, tick sequence, spread, replay, or exact intrabar fidelity from current imported bars.

## Validation limitation
The environment still lacks the .NET SDK/compiler and Android/Windows SDKs. Native compilation and device validation are not claimed.

## Next build sequence
1. Deterministic SMA and ATR evaluation.
2. Explicit numeric stop/target/position-sizing semantics.
3. Causal event-driven execution-plan adapter with MTF support.
4. Multi-script/multi-instrument isolation.
5. Canonical semantic fingerprints linked to durable research jobs/evidence.
6. Generated indicator/strategy artifact lineage.

## Permanent UI rule
Visual refinement remains frozen until core application functionality is substantially runnable. Preserve current UI work as a later refinement target.

## Permanent documentation rule
Every build must ship with the updated User Manual, standalone handoff, comprehensive build summary, release metadata, validation results, source hashes, limitations, fidelity boundaries, and next-build sequence.

Continue the Master Build from v20.14 without asking the user to restate the architecture.
