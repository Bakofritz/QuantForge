# QuantForge Futures Research Platform — User Manual v20.4

## Purpose
QuantForge is a governed futures research and backtesting platform. v20.4 strengthens durable research execution and crash recovery while keeping the application research-only.

## Research job lifecycle
A research job progresses through Queued → Running → Completed, or through Paused, Failed, Canceled, or RecoveryRequired as appropriate.

An execution lease identifies which device/process currently owns mutable execution. The lease is separate from the durable job record.

## Checkpoints and recovery
A checkpoint records the exact research cursor plus dataset, configuration, and engine fingerprints. After a crash or lease expiry, the system must validate those fingerprints before continuing. A changed dataset, configuration, or engine creates a new research condition rather than silently resuming the old one.

## Cancellation and timeout behavior
Cancellation is persisted so another process can observe it. Lease expiry produces a recovery condition rather than an automatic continuation. Terminal outcomes should be recorded as append-only research events.

## Event-driven research boundary
The intended pipeline is:
Market Event Stream → Strategy Observation → Simulation Order → Execution Model → Fill → Ledger/Analytics → Evidence.

The current supplied OHLCV data supports bar-based simulation only. Exact tick order, bid/ask spread, and intrabar sequencing require corresponding event data.

## Strategy safety
Strategies receive a bounded event-time context. They do not receive future events. The event engine enforces chronological processing and the strategy interface is separated from execution simulation.

## AI and bot boundary
AI analysis remains non-authoritative. It may analyze governed research results but cannot place live orders, enable live trading, mutate canonical data, alter manifests, or execute undeclared code. Bounded bots remain subject to policy and manifest controls.

## Visual/UI status
The radial interface is preserved as the current reference implementation. v20.4 intentionally does not prioritize visual refinement; functional runtime completion takes precedence.

## Current limitations
- Native .NET/MAUI compilation cannot be certified in the present environment because the required SDK toolchain is unavailable.
- Android/Windows device execution is not certified by this build.
- Native SQLite runtime execution is not certified by this build.
- Multi-timeframe causal aggregation is not yet complete.
- Tick/replay execution fidelity is not yet implemented.
- Native script scrubber/quarantine migration is not yet complete.

## Safety and data fidelity
Research results are simulations. OHLCV data remains OHLCV. No component may infer bid/ask or tick sequencing that was not actually supplied.

## Standard build rule
Every QuantForge build must ship with an updated detailed User Manual, standalone handoff, release metadata, validation record, limitations, next steps, data-fidelity boundaries, and safety/authority boundaries.
