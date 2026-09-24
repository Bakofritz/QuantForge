# QuantForge Futures Research Platform — User Manual v20.5

## What changed
v20.5 adds a mandatory canonical-data gate to the native research path and introduces causal multi-timeframe observation.

## Canonical data gate
Before native research execution, QuantForge verifies:
- dataset exists and is non-empty;
- instrument and timeframe identity are present;
- OHLC relationships are valid;
- timestamps are strictly chronological and unique;
- OHLCV fidelity is actually present.

Warnings record when execution fidelity is limited to OHLCV. Warnings do not upgrade the data.

## Causal multi-timeframe observation
Strategies may receive synchronized views for configured timeframes. A timeframe view contains only source bars whose timestamps are at or before the current event time. A forming bucket is marked incomplete. Future completed bars are never exposed to the strategy.

This is a causal observation layer, not tick/replay fidelity. Exact intrabar ordering remains unavailable when the source is OHLCV.

## Research safety
The native research path remains simulation-only. No live orders, live trading, arbitrary code execution, or unrestricted application mutation is introduced.

## Build/runtime status
The source architecture is prepared for .NET 10/MAUI. Native compilation, APK generation, Windows packaging, and device runtime validation remain blocked until the required SDK toolchain is available.

## Data fidelity
Supplied OHLCV remains OHLCV. QuantForge does not infer bid/ask, tick sequencing, or replay events.

## Research interpretation
Backtest and optimization results are evidence for research, not guarantees. Preserve dataset fingerprint, strategy definition, configuration, engine version, and data-fidelity declaration with every reproducible run.
