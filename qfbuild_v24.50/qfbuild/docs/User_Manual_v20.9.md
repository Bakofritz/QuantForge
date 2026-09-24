# QuantForge User Manual v20.9

## Release purpose
v20.9 makes the Monte Carlo research operation interruption-safe. Long-running bootstrap jobs are divided into bounded chunks and their state is persisted locally.

## Running Monte Carlo
1. Select an immutable local dataset.
2. Confirm instrument and timeframe.
3. Select the strategy and parameters.
4. Choose the iteration count and deterministic seed.
5. Start the research job.
6. QuantForge acquires a device execution lease.
7. Work executes in bounded chunks.
8. Progress, aggregation state, and an integrity-bound checkpoint are persisted after each chunk.
9. The lease is renewed before the next chunk.
10. When all iterations finish, the report and evidence record are finalized.

## Resume behavior
Re-running the same job can resume from the persisted progress record. QuantForge validates dataset, configuration, engine, iteration-total, and progress integrity before resuming.

If any fingerprint differs, the stored progress is rejected rather than silently reused.

## Cancellation
Cancellation is durable. A canceled job does not emit completion evidence. The current checkpoint remains available for audit but is not automatically treated as completed work.

## Multi-device behavior
A job has one active execution lease at a time. Another device cannot execute the same job while the lease is active. After a lease expires, a later invocation may reclaim the job only after validating the stored research fingerprints.

Different job IDs may represent different strategies, instruments, timeframes, or parameter configurations and can execute independently on separate devices.

## Interpretation of Monte Carlo results
Bootstrap results describe the declared resampling experiment over observed completed-trade PnL. They are not forecasts or guarantees of future live performance.

## Data fidelity
Current native imported datasets are OHLCV. Monte Carlo cannot create tick sequencing, bid/ask, spread, or intrabar execution information that was not present in the source data.

## Safety
Research operations have no live-order or live-trading authority. Imported code remains subject to the governed script acquisition/scrubbing architecture before execution authority is considered.

## Current validation limitation
The source has been structurally validated, but this environment does not currently contain the .NET/MAUI, Android, and Windows SDK toolchains required for native compilation and device validation.
