# QuantForge User Manual v20.8

## Monte Carlo / Bootstrap
Monte Carlo analysis in v20.8 resamples the completed-trade PnL observations produced by the deterministic backtest. It does not create synthetic market data or upgrade OHLCV fidelity.

## Running a study
1. Select a governed research dataset.
2. Confirm instrument and timeframe identity.
3. Select the strategy configuration.
4. Set the iteration count and deterministic seed.
5. Run the governed Monte Carlo job.
6. Review observed PnL alongside bootstrap mean, median, 5th/95th percentiles, drawdown statistics, and sample probabilities.
7. Retain the report fingerprint and evidence record for reproducibility.

## Interpreting probabilities
`ProbabilityNonPositivePnl` means the fraction of generated bootstrap samples whose resampled terminal PnL is non-positive.

`ProbabilityDrawdownAtLeastObserved` means the fraction of bootstrap samples whose maximum drawdown is at least the observed baseline drawdown.

These are properties of the declared resampling experiment, not forecasts or guarantees about future live trading.

## Reproducibility
The same dataset fingerprint, strategy configuration, iteration count, and non-zero seed reproduce the same deterministic bootstrap sequence and report fingerprint.

## Safety
Research-only. No live orders, live trading, arbitrary code execution, settings mutation, or canonical market-data mutation is available through this job.

## Fidelity
Current native source data is OHLCV. No tick, bid/ask, spread, or intrabar sequencing is claimed.

## Limitations
The current method is a simple trade-outcome bootstrap. It does not model serial correlation, regime transitions, or market-path dependence. v20.9 will add durable iteration-level recovery and stronger multi-job isolation.
