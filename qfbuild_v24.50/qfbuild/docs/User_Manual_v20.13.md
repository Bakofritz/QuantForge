# QuantForge User Manual — v20.13

## What changed
v20.13 adds the first governed bridge from a persisted canonical strategy model into deterministic research artifacts.

## Workflow
1. Import source.
2. Quarantine it.
3. Audit it without execution.
4. Select permitted components.
5. Commit the canonical research model.
6. Create a canonical research plan.
7. Review supported and unsupported semantics.
8. Evaluate supported indicators/signals against canonical market data.
9. Only then connect supported plans to backtesting.

## Important fidelity rule
If the source audit detects an EMA and a crossover but does not establish the exact EMA periods, QuantForge must not claim those periods came from the source. v20.13 therefore uses explicit adapter defaults for the current EMA crossover demonstration and records their provenance.

## Indicator output
The adapter can produce:
- deterministic EMA series;
- deterministic crossover events;
- timestamped chart markers;
- fingerprints for reproducibility.

These outputs are research artifacts. They do not place orders.

## Unsupported behavior
Unsupported or ambiguous canonical features are reported. They are not silently replaced with approximate behavior.

## Safety
Imported source remains inert text. v20.13 does not compile, load, invoke, or execute imported source. Live deployment remains disabled.

## Data fidelity
Current native research is OHLCV bar-driven. This does not establish bid/ask, spread, tick sequencing, Market Replay fidelity, or exact intrabar execution.

## Validation status
Source-level validation is complete. Native Windows/Android compilation and device validation require an environment containing the .NET/MAUI and platform SDK toolchains.
