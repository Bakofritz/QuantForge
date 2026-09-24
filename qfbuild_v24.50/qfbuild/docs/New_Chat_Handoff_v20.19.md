# QuantForge v20.19 New-Chat Handoff

Current baseline: v20.19.

v20.19 adds the deterministic research account ledger, evolving equity/drawdown tracking, daily loss locking, signed long/short ledger support, and explicit OHLCV intrabar ambiguity resolution. The default ambiguity policy is conservative stop-first.

Preserve the standing governed defaults: $3,000 initial account, MES primary, MNQ planned, one-contract initial limit, 6-point stop, trailing activation +4 / distance 2, target disabled by default, $0.95/side simulation commission assumption, one tick/side slippage, and explicit connectivity-latency scenarios.

Visual refinement remains deferred. Continue core runtime, data integrity, governance, backtesting, storage, synchronization, AI/Scout safety and full runtime validation.

Next build priorities:
1. Integrate AccountLedger into EventDrivenBacktestEngine.
2. Enforce exactly one exit per open position per event/bar.
3. Make stop/target/trailing/strategy-exit precedence explicit and auditable.
4. Persist complete ledger and execution-sensitivity reports as durable research evidence.
5. Add a separate MNQ instrument profile without enabling it as primary.
6. Add measured-latency telemetry contracts rather than treating estimates as measurements.
7. Strengthen short-side execution model and risk checks.
8. Native .NET/MAUI compilation and Windows/Android validation when SDKs are available.

Validation boundary: source/static validation passed; native compilation/device validation was not available.
