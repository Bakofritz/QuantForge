# QuantForge User Manual — v20.39

## What v20.39 does
v20.39 adds a controlled validation path for the local SQLite storage layer. It does not enable parallel research execution.

## Storage contention validation
A future native runtime can construct `SqliteConcurrencyValidationAdapter` with a local SQLite database path. The adapter uses independent storage instances to test:
1. lease acquisition races;
2. identical receipt append idempotency;
3. contradictory receipt rejection;
4. checkpoint immutability;
5. cross-connection reads after writes.

## Validation result records
The resulting fingerprinted validation record can be persisted in `concurrency_validation_results` and linked into the evidence/event chain through `ResearchRuntime.ValidateStorageConcurrencyAsync(...)`.

## Checkpoint behavior
A checkpoint identified by the same job and sequence is immutable. Re-submitting identical content is accepted idempotently. Re-submitting different content is rejected.

## Safety
No live orders, live trading, unrestricted script execution, canonical data mutation, or AI policy mutation is introduced by v20.39. Parallel research remains disabled until native runtime/resource validation supports enabling it.

## Deferred User Contributions
None are needed for this build. Follow the established code words when later validation stages require NT8, MES/MNQ, replay, strategy, rollover, or latency material.
