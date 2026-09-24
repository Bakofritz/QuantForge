# QuantForge Futures Research Platform — New-Chat Handoff v20.55

## Continue point
Continue the Master Build from **v20.55 — Native Recovery Continuation Gate Integrity**.

## Current architecture
QuantForge is a native cross-platform .NET 10 / .NET MAUI Windows + Android application with shared Core/Data/Runtime/Backtesting/Storage/Governance/Bots/AI/Scout/Reporting layers. The former HTML radial UI remains a preserved transitional/reference artifact. Visual refinement is currently frozen while core functionality is completed.

## v20.55 completed
- Durable recovery binding reconciliation is now an explicit continuation authorization boundary.
- `RecoveryContinuationGateService` prepares recovery, performs binding reconciliation, verifies durable audit/evidence/event records, and only then authorizes continuation.
- Failure or incomplete persistence causes fail-closed blocking.
- Blocked recovery returns the running job to `RecoveryRequired` and releases the recovery lease.
- `CheckpointBoundContinuationService` uses the gate before invoking the user/domain continuation callback.
- Native SQLite verification interface and implementation added.
- Runtime public gate entry point added.
- Architecture checks added.

## Validation status
Structural C# validation passed for 139 files. .NET SDK/runtime remains unavailable, so no native compilation, SQLite execution, Windows execution, Android execution, or device validation is claimed.

## Next build: v20.56
Build the **Recovery Gate Fault-Injection Continuation Harness**. It should prove, with deterministic test-only stores/harnesses, that the continuation callback is never invoked when:
- reconciliation persistence fails;
- reconciliation is incomplete;
- read-back fails;
- checkpoint binding changes;
- lease binding changes or expires;
- reconciliation conflicts;
- a crash occurs after detection but before durable authorization.

It should also verify that successful reconciliation + successful read-back permits exactly the intended continuation path and that retries remain idempotent.

## Standing rules
- Do not ask whether to continue ordinary engineering.
- Stop only for genuine blockers, credentials, destructive actions, material architecture/security-boundary changes, or irreconcilable ambiguity.
- No live trading, live orders, unrestricted AI mutation, canonical data mutation, undeclared code execution, or silent policy/manifest changes.
- Every release must include User Manual, New-Chat Handoff, Comprehensive Build Summary, validation, release metadata, limitations, next steps, hashes, and explicit safety/data-fidelity boundaries.
- Defer NT8/data/user contributions until their relevant phase.
