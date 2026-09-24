# QuantForge User Manual — v20.42

## 1. What v20.42 adds
v20.42 validates a controlled failure-and-retry boundary for the native SQLite storage layer.

## 2. Fault points
The validation layer can deliberately inject a controlled fault into:
- lease acquisition;
- lease renewal;
- checkpoint persistence;
- execution receipt persistence;
- evidence persistence;
- concurrency validation-result persistence;
- research job-state persistence.

## 3. Failure/retry workflow
For each fault point:
1. Open a real SQLite store with a validation-only fault plan.
2. Attempt the selected storage operation.
3. Require the configured fault to be explicitly observed.
4. Open a clean storage instance without fault injection.
5. Retry the same logical operation.
6. Verify that the expected durable record exists.

The validation does not assume that the failed call partially succeeded.

## 4. Why this matters
A research system that supports checkpoints, leases, receipts, and recovery must distinguish a failed write from a successful write. Otherwise it could incorrectly believe that a lease, checkpoint, receipt, or job state was committed when it was not.

## 5. Runtime use
The runtime exposes `ValidateNativeStorageFaultRecoveryAsync(databasePath)` for controlled validation environments.

This method is not a research execution command and does not grant trading authority.

## 6. Production behavior
The normal `SqliteLocalStore(string databasePath)` constructor does not configure fault injection. The fault seam is supplied only through an internal/test-oriented constructor.

## 7. Recovery boundary
A successful retry does not silently resume an arbitrary research job. It proves only that the selected storage operation can be retried and that the expected durable state can be verified afterward.

## 8. Safety
QuantForge remains research/backtesting software. Live trading and live order authority remain unavailable. AI remains non-authoritative. Parallel multi-case execution remains disabled pending native validation.

## 9. Data fidelity
No market-data fidelity is increased by this release. OHLCV remains OHLCV unless validated higher-fidelity data is later supplied.

## 10. Known limitation
Native .NET/SQLite execution has not been performed in the current build environment. Source and architecture validation are therefore distinct from native runtime validation.

## 11. Next
The next phase connects storage-failure behavior to worker/lease/recovery state transitions and terminal-result integrity.
