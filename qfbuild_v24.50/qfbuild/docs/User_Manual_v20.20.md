# QuantForge v20.20 User Manual

## NT8 Data Validation Workflow
1. Point QuantForge at the Windows NinjaTrader 8 data root, normally the user's Documents\NinjaTrader 8 data area.
2. Run Source Inventory. QuantForge records relative paths, file sizes, timestamps, SHA-256 fingerprints, and source classification.
3. Select the instrument/contract and date range.
4. Generate the validation plan.
5. Supply a small representative Tick export from NinjaTrader for reference comparison.
6. Supply longer Minute and Day exports when available.
7. Compare native/derived data against the exports.
8. Only datasets passing the canonical data gate become research-eligible.

## Recommended sample strategy
A few complete tick sessions are enough for an initial parser/normalizer validation. Prefer 3–5 sessions with different market conditions. More days increase confidence but are not required to validate a decade of minute/day continuity.

For long-history validation, use Minute and Day exports across the full requested period. This is intentionally much smaller than downloading the corresponding tick archive.

## What a PASS means
PASS means the compared data agrees within the configured comparison rules for the fields tested. It does not establish accuracy for periods or fields that were not compared.

## Native Replay caution
Do not label an imported file as Market Replay fidelity merely because it resides in an NT8 data directory. The native parser must identify the actual format and fields. Unknown formats remain quarantined.
