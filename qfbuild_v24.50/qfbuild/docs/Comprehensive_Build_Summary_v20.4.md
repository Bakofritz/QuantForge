# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.4

## 1. Build identity
- Release: v20.4
- Baseline: v20.3
- Focus: crash-safe research lifecycle + event/strategy boundary
- Native target: C# / .NET 10 LTS + .NET MAUI for Windows and Android
- Visual state: intentionally frozen; no visual refinement added in this build.

## 2. Engineering objective
v20.4 makes a long-running research job durable independently of its current execution lease and introduces a reusable market-event → strategy → simulation boundary. This prevents crash recovery from being tied to a particular process and prevents future MES/MNQ, multi-timeframe, replay, optimization, and robustness work from being hard-wired to one strategy runner.

## 3. Implemented
### Durable job lifecycle
States: Queued, Running, Paused, Completed, Failed, Canceled, RecoveryRequired.

### Durable checkpoint
Each checkpoint stores:
- job ID;
- monotonic sequence;
- event/bar cursor;
- dataset fingerprint;
- configuration fingerprint;
- engine fingerprint;
- deterministic state hash;
- saved timestamp.

A checkpoint is eligible for resume only when all three fingerprints match the current job definition.

### Durable cancellation
Cancellation requests are persisted rather than held only in process memory. This permits a replacement process/device to observe the request after recovery.

### Event-driven research boundary
Introduced:
`MarketEvent → StrategyContext/Strategy → SimulationOrder → ExecutionModel → Fill`

The boundary is intentionally strategy-agnostic. The current OHLCV EMA adapter remains a compatibility path, not the final execution-fidelity model.

### Governance
The event engine has no live-order or live-trading authority. Simulation operations are explicit; prohibited authorities remain outside the research engine.

## 4. Key design tradeoff
The engine does not automatically resume an expired job. Recovery first requires a valid checkpoint, matching immutable fingerprints, and a newly acquired execution lease. This is conservative by design: it avoids silently continuing work under changed inputs or after another device has legitimately reclaimed the job.

## 5. Validation
### Source validation
- C# files structurally inspected for balanced declarations/braces.
- Required v20.4 contracts, storage tables, lifecycle states, checkpoint fields, cancellation persistence, event/strategy interfaces, and authority boundaries verified by scripted checks.
- Deterministic fingerprint and causal ordering logic reviewed.

### Environment limitation
The build environment still does not provide the .NET SDK/compiler, Android SDK, or Windows SDK. Therefore this release does **not** claim native compilation, APK generation, Windows packaging, device execution, or native SQLite runtime certification.

## 6. Data fidelity
Current native importer remains OHLCV. It does not create bid/ask, tick sequencing, or replay fidelity that was not present in the source data.

## 7. Safety boundaries
No live trading, live orders, arbitrary code execution, canonical-data mutation, manifest mutation, or unrestricted AI application mutation is introduced.

## 8. Visual boundary
The current radial UI/reference work is preserved. No new visual changes were made because core runtime functionality is now the priority.

## 9. Mandatory documentation
This release includes the updated User Manual, standalone New-Chat Handoff, release manifest, validation record, and this comprehensive engineering log.

## 10. Next engineering phase
1. Make the canonical data gate mandatory before every research execution path.
2. Add multi-timeframe causal event aggregation.
3. Add explicit execution models for OHLCV bar-close, declared intrabar assumptions, and later tick/replay fidelity.
4. Move optimization/walk-forward/robustness jobs onto the durable job/checkpoint infrastructure.
5. Port the first-line script scrubber and quarantine into native services.
6. Establish native build environment validation and execute actual Windows/Android runtime tests when the required SDKs are available.
