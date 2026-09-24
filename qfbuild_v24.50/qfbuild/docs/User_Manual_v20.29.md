# QuantForge Futures Research Platform — User Manual v20.29

## 1. Purpose
v20.29 adds governed resource controls to multi-case research execution. The goal is to prevent a case from running beyond its declared orchestration limits and to preserve a verifiable receipt for each case.

## 2. Resource policy
A multi-case request may declare:
- maximum case duration;
- maximum batch duration;
- lease heartbeat interval;
- maximum concurrency.

The current coordinator requires maximum concurrency to remain `1`.

## 3. Lease heartbeat
While a case is executing, QuantForge periodically renews its job lease. If renewal fails, the execution path stops rather than continuing without a valid lease.

## 4. Case timeout
Each case receives a linked cancellation token. When its maximum duration expires, the case is stopped and recorded as a resource timeout rather than as a normal successful completion.

## 5. Execution receipts
A completed or stopped case produces an execution receipt containing:
- batch ID;
- context ID;
- job ID;
- state;
- result fingerprint when available;
- receipt fingerprint;
- elapsed time;
- heartbeat renewal count;
- resource status;
- error code when applicable.

Receipts are recorded through the append-only evidence/event boundary when the evidence store is available.

## 6. Interpretation
A receipt documents what the research coordinator observed. It does not prove that the underlying strategy or market data is correct, profitable, or suitable for live trading.

## 7. Data fidelity
OHLCV remains OHLCV. QuantForge does not infer bid/ask, tick sequencing, or replay fidelity from bar data.

## 8. Safety
This release does not provide live trading, broker order routing, arbitrary imported-code execution, canonical market-data mutation, or unrestricted AI application mutation.

## 9. Native runtime status
The current development environment lacks the required .NET, Android, and Windows SDK toolchains. Source-level validation is therefore separate from native compilation and device validation.

## 10. Deferred contributions
No user contribution is required for this release. NT8 replay/tick/minute/daily and latency samples remain deferred until the corresponding validation stage.

## 11. Next build
v20.30 will connect resource receipts more tightly to durable checkpoints and batch accounting, then prepare the architecture for a controlled concurrency review.
