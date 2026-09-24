# QuantForge v20.90 User Manual
## Governed Backtest Case Factory & Deterministic Research Batch Execution

### Purpose
v20.90 provides the first explicit bridge from a governed research batch into deterministic backtest execution. It does not create live trading authority.

### Normal flow
1. Create immutable `ResearchContextDescriptor` objects.
2. Bind each context to its dataset fingerprint, strategy identity, instrument, timeframe, and configuration.
3. Create a `GovernedBacktestCase` for each approved context.
4. Supply a `GovernedBacktestBatchRequest`.
5. The service verifies read-only authority, resource policy, native runtime qualification, and case counts.
6. Each dataset is loaded and its content fingerprint is compared with the context fingerprint.
7. The deterministic backtest engine runs the approved strategy parameterization.
8. Results are sorted by ContextId and fingerprinted deterministically.

### Safety behavior
- Unqualified native runtime: batch denied.
- Unknown resource estimate: batch denied by resource admission.
- Memory budget exceeded: batch denied.
- Dataset fingerprint mismatch: execution stops.
- Instrument/timeframe mismatch: execution stops.
- Invalid strategy parameters: execution stops.
- Read-only authority disabled: execution stops.
- No API in this layer submits live orders.
- No API in this layer interprets arbitrary source code.

### Current deterministic engine
The current governed batch uses the existing EMA crossover backtest engine. Its fidelity declaration remains OHLCV bar-driven; it does not claim tick ordering, bid/ask, spread, or intrabar sequencing fidelity.

### Runtime qualification
Structural validation is not the same as native runtime qualification. This environment does not contain the .NET SDK, so the build does not claim native .NET execution.

### Operational note
The batch service is a research execution boundary. Lease lifecycle, terminal receipts, checkpointing, and recovery controls remain owned by the surrounding governed execution architecture and are not bypassed by this service.
