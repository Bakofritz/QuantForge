# QuantForge User Manual v20.28

## What changed
v20.28 adds durable orchestration for multi-case research batches.

## Running a multi-case batch
1. Create independent research contexts.
2. Give every case a deterministic context identity.
3. Submit the bounded batch.
4. QuantForge acquires a separate lease for each case.
5. Each completed case is persisted before the next case proceeds.
6. If execution is interrupted, the batch checkpoint identifies completed terminal cases.
7. On recovery, QuantForge verifies the batch fingerprint and progress hash.
8. Completed cases are skipped rather than silently rerun.
9. The unresolved case is handed back to its case-level recovery path.

## Evidence and reproducibility
Batch state is bound to the complete set of case identities, dataset fingerprints, strategy fingerprints, instrument/timeframe identities, configuration fingerprints, and engine fingerprints.

## Safety
The multi-case coordinator is research-only. It cannot place live orders or grant imported scripts live authority.

## Important limitation
v20.28 does not provide validated parallel execution. Parallel scheduling is deferred until resource accounting and native runtime validation are available.

## Data fidelity
Checkpointing does not change market-data fidelity. OHLCV remains OHLCV; no bid/ask, tick, or replay information is inferred.

## Deferred User Contributions
No contribution is required for v20.28.
