# QuantForge User Manual — v20.23

## Core research execution model
QuantForge now represents strategy intent with a canonical signed position contract while retaining compatibility with existing long-only strategy observations.

### Position states
- **Flat** — no open research position.
- **Long** — positive signed quantity.
- **Short** — negative signed quantity.

### Position intents
A strategy may describe:
- Hold
- Enter Long
- Enter Short
- Exit
- Reverse to Long
- Reverse to Short

These are research intents only. They are not broker orders.

## Side-aware fills
QuantForge applies adverse modeled slippage by side:
- Long entry is filled above the reference price.
- Short entry is filled below the reference price.
- Long exit is filled below the reference price.
- Short exit is filled above the reference price.

Commission remains a separate configured cost.

## OHLCV limitation
OHLCV bars do not reveal the exact sequence of intrabar events. If a stop and target can both be reached within the same bar, QuantForge uses the declared ambiguity policy rather than inventing a tick sequence.

The default policy remains ConservativeStopFirst.

## Legacy strategies
Existing strategies using `EnterLong` and `ExitLong` remain supported through a compatibility adapter. They are not silently converted into short-side behavior.

## Governance
Imported source remains inert until it passes the existing quarantine and static scrub/audit workflow. Canonical strategy artifacts remain research-governed. AI remains non-authoritative.

## Data fidelity
Do not interpret modeled fills, latency scenarios, or OHLCV ambiguity policies as observed broker/exchange execution. Higher-fidelity claims require corresponding validated event data.

## Native runtime status
The current engineering environment lacks the .NET/MAUI, Android, and Windows runtime toolchain required for native compilation and device validation. This build therefore carries source-level validation only.

## Deferred contribution codes
- **NT8 REPLAY** — native replay sample.
- **NT8 TICK** — matching tick export.
- **NT8 MINUTE** — long-range minute export.
- **NT8 DAILY** — long-range daily export.
- **REPLAY MATCH** — paired replay/export sample.
- **LATENCY DATA** — measured latency observations.
