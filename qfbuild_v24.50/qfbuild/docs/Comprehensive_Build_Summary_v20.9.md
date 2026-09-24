# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.9

## 1. Build identity
- Release: v20.9
- Baseline: v20.8
- Focus: durable, interruption-safe Monte Carlo execution with persistent iteration progress
- Native target: C# / .NET 10 LTS + .NET MAUI, Windows + Android
- Visual work: frozen
- PDF analysis: deferred

## 2. Engineering work
### Durable research progress
Added `ResearchProgress` as a durable, fingerprint-bound progress record containing operation, cursor, total work, dataset/configuration/engine fingerprints, serialized aggregation state, integrity hash, and timestamp.

### SQLite persistence
Added `research_progress` with `(job_id, operation)` as its identity. Progress is persisted independently of the older checkpoint cursor so an operation can retain the aggregation state needed to resume without recomputing prior chunks.

### Resumable Monte Carlo
The Monte Carlo analyzer now supports:
- deterministic state creation;
- bounded `RunChunk()` execution;
- persisted RNG state;
- persisted sampled net-PnL and drawdown observations;
- deterministic final aggregation;
- state serialization/deserialization.

The runtime executes 250-iteration chunks, renews the device lease before each chunk, checks durable cancellation, persists progress and a research checkpoint after each chunk, and only finalizes evidence after the complete iteration set is present.

### Conservative recovery
On a later invocation, the runtime loads the existing job/progress record and refuses to resume if any of these differ:
- dataset fingerprint;
- configuration fingerprint;
- engine fingerprint;
- requested iteration total;
- progress integrity hash.

This prevents a stale or altered checkpoint from being interpreted as valid research state.

### Job isolation
Configuration fingerprints include strategy, instrument, timeframe, parameters, iteration count, seed, point value, and commission. Jobs therefore cannot silently reuse progress from a materially different research configuration.

## 3. Cancellation / lease behavior
Each chunk:
1. checks cancellation;
2. renews the execution lease;
3. executes bounded work;
4. persists aggregation state;
5. persists a checkpoint.

Cancellation transitions the durable job to `Canceled`. Lease release occurs independently of the cancellation token so interrupted jobs do not remain unnecessarily leased until expiry.

A failed lease renewal stops execution conservatively rather than continuing without ownership.

## 4. Evidence boundary
No completion evidence is emitted until all requested Monte Carlo iterations have completed. Existing evidence identifiers remain idempotent through the evidence store's `INSERT OR IGNORE` behavior.

## 5. Data-fidelity boundary
The analysis remains bootstrap resampling of an OHLCV bar-driven deterministic backtest. Persistence and resampling do not create bid/ask, tick sequencing, spread, intrabar ordering, or replay fidelity.

## 6. Safety boundary
No live trading, live order placement, arbitrary code execution, application-setting mutation, manifest bypass, or canonical market-data mutation was introduced.

## 7. Validation
- 33 C# files structurally balanced.
- Progress persistence: PASS.
- SQLite progress table: PASS.
- Fingerprint-bound resume: PASS.
- Progress integrity verification: PASS.
- Cancellation check: PASS.
- Lease renewal: PASS.
- Bounded chunk execution: PASS.
- Durable checkpoint persistence: PASS.
- Final evidence event: PASS.
- No live-order authority in new research path: PASS.

Native .NET/MAUI, Android, and Windows compilation/device validation remains unavailable in the current environment because the required toolchains are not installed.

## 8. Known limitations
- v20.9 fully wires resumable chunk execution into the Monte Carlo path; optimization, robustness, and walk-forward still need their operation-specific resumable aggregators.
- Persisting all bootstrap samples is intentionally simple and deterministic but can consume substantial local storage for large iteration counts.
- The simple bootstrap still assumes resampling of observed completed-trade outcomes; it does not model serial dependence or new market paths.
- Native runtime validation remains pending.

## 9. Next phase
v20.10 should generalize the durable chunk executor to optimization, robustness, and walk-forward, add compact percentile/distribution storage where appropriate, and introduce fault-injection tests around every persistence boundary. After that, proceed to the canonical strategy/indicator model and native first-line script scrubber/quarantine integration.
