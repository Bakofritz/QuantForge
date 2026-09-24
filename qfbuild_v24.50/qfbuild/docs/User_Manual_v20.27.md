# QuantForge User Manual v20.27

## 1. Purpose
v20.27 introduces the governed coordinator used to execute multiple independent research cases as one batch while preserving case-level state and evidence boundaries.

## 2. Batch model
A batch has a batch ID and contains multiple immutable research-context descriptors. Each descriptor identifies its dataset, strategy, instrument, timeframe, configuration, and engine lineage.

## 3. Case isolation
Every case executes with its own `ResearchContextState`. Mutable strategy values, position state, and evidence references are not shared with another case.

## 4. Execution leases
Each case acquires its own execution lease using its JobId. A lease conflict blocks that case instead of silently sharing execution ownership.

## 5. Execution order
v20.27 executes cases sequentially. This is deliberate. Parallel execution is not claimed until concurrency, resource accounting, native runtime behavior, checkpoint recovery, and device coordination are validated.

## 6. Results
Each case returns its own result fingerprint and lifecycle state. The coordinator then creates an aggregate fingerprint from the batch identity and per-case outcomes. The aggregate does not replace individual case evidence.

## 7. Failure and cancellation
A failed case is recorded as failed without converting it into a successful result. Cancellation stops at a case boundary, records the active case as canceled, and releases its lease.

## 8. Durable jobs
When an `IResearchJobStore` is supplied, the coordinator records queued, running, completed, failed, and canceled lifecycle states. Domain-specific checkpoint details remain owned by the underlying research engine until v20.28 adds coordinator-level checkpoint persistence.

## 9. Safety
The coordinator has research authority only. It does not place orders, route to brokers, execute arbitrary imported source, mutate canonical market data, or grant AI application mutation authority.

## 10. Fidelity
Multi-case orchestration does not increase market-data fidelity. OHLCV remains OHLCV. Higher-fidelity claims require actual validated tick, bid/ask, or replay events.

## 11. Current validation limitation
The current environment does not provide the .NET SDK/compiler, Android SDK/device, or Windows native runtime. Source-level validation is therefore reported separately from native runtime validation.

## 12. Deferred contributions
Use the established codes only when requested at the appropriate stage: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, REPLAY MATCH, LATENCY DATA.
