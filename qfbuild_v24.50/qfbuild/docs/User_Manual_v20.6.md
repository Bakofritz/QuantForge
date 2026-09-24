# QuantForge User Manual v20.6

## What v20.6 adds
QuantForge v20.6 adds deterministic parameter optimization and chronological walk-forward analysis to the native research architecture.

## Optimization
An optimization job defines bounded parameter ranges. QuantForge evaluates every valid combination independently and records each case. The source dataset is not modified by optimization.

Example concept:
- Fast EMA: 5–15, step 1
- Slow EMA: 20–40, step 2

Cases where Fast >= Slow are excluded because they do not satisfy the declared EMA-cross relationship.

## Walk-forward analysis
A walk-forward job divides data into chronological training and test windows.

For each window:
1. Train data is isolated.
2. Parameter search runs only on train data.
3. A deterministic training selection rule identifies the selected configuration.
4. That configuration is run on the following test window.
5. The test result is recorded separately from training results.

This prevents the test window from being used to select its own parameters.

## Important interpretation rule
A strong optimization result is not automatically a strategy recommendation. Review:
- data quality;
- execution assumptions;
- parameter sensitivity;
- drawdown;
- trade count;
- out-of-sample behavior;
- regime dependence;
- robustness and resampling results.

## Data fidelity
Current native execution remains OHLCV bar-driven. It does not claim:
- exact historical tick sequence;
- bid/ask execution;
- spread reconstruction;
- intrabar order sequencing;
- exchange-level event fidelity.

Those capabilities require corresponding source data and execution models.

## Safety
Optimization and walk-forward analysis are research-only. They cannot place live orders, alter canonical market data, or silently modify application policy.

## Build validation
v20.6 passed source-level structural and contract checks. Native .NET/Android/Windows execution is not claimed until a suitable toolchain is available.

## Next
The next release sequence expands robustness, checkpointed research jobs, multi-script/multi-instrument isolation, and canonical strategy/indicator generation.
