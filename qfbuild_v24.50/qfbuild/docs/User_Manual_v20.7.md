# QuantForge User Manual v20.7

## Robustness analysis
Run a bounded perturbation around a selected strategy configuration. Each tested parameter pair is independently backtested against the same immutable dataset fingerprint.

## Interpretation
The report records raw case metrics, a stability fingerprint, and retention metadata. These are research measurements, not guarantees of future performance.

## Checkpointing
A robustness job records its dataset, configuration, and engine fingerprints before execution. Future recovery must validate those identities before continuing.

## Safety
Research jobs remain sandboxed from live trading, order placement, canonical-data mutation, application-setting mutation, and arbitrary code execution.

## Fidelity
Current native research uses imported OHLCV bars. No tick, bid/ask, spread, or intrabar sequencing is claimed unless a corresponding event source is present.

## Current limitations
Native compilation and device validation remain blocked by missing .NET/MAUI/Android/Windows SDKs in the current build environment.
