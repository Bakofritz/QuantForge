# QuantForge User Manual — v20.41

## 1. Purpose
v20.41 connects controlled storage-failure testing to the actual SQLite storage implementation.

## 2. Normal operation
The normal `SqliteLocalStore` constructor does not configure a fault injector. Ordinary application storage is unchanged.

## 3. Validation operation
A validation-only adapter can open a real SQLite database and intentionally inject one controlled failure at a selected storage point.

Supported points:
- Lease acquisition
- Lease renewal
- Checkpoint write
- Execution receipt write
- Evidence write
- Validation-result write
- Research job-state write

## 4. Expected behavior
An injected fault must surface as an explicit `StorageFaultInjectedException`. The harness records the observation rather than treating the operation as successful.

## 5. Native validation boundary
The adapter is designed for actual .NET/SQLite execution. Source-level validation in an environment without the native .NET toolchain does not count as a native execution pass.

## 6. Safety boundary
This feature does not enable parallel research, live orders, live trading, unrestricted AI mutation, or canonical-data mutation.

## 7. Next step
Future validation will test whether each failure leaves durable state coherent and whether recovery/retry is permitted only after the required fingerprint and lease checks.

## 8. Deferred User Contributions
No user contribution is required for v20.41. NT8, MES/MNQ, replay, rollover, strategy-source, and measured-latency material remains deferred until its corresponding validation stage.
