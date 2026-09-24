# QuantForge v20.44 — Transactional Terminal Commit Boundary + Recovery Integrity Harness

## Scope
v20.44 hardens the terminal research outcome boundary after v20.43 identified that independently persisted job state, receipts, evidence, and events could still produce partial-commit ambiguity.

## Implemented
- Added Core `ITerminalCommitStore` and terminal commit contracts.
- Added SQLite transactional terminal commit implementation.
- Terminal commit atomically persists, within one SQLite transaction:
  - terminal research job state
  - execution receipt
  - evidence record
  - terminal research event
- Terminal job mutation is conditional on the expected worker's unexpired lease.
- Existing resource receipt identity remains immutable/idempotent.
- Storage fault injection points are honored inside the transaction at job-state, receipt, and evidence phases; an injected exception prevents transaction commit.
- Multi-case coordinator uses the transactional path when the storage implementation supports it, otherwise retains the governed fallback path.
- Failure/cancellation terminal-job object is now explicitly passed to the terminal commit path.
- Added architecture validation for the transactional terminal commit contract.

## Engineering rationale
The objective is not to claim that every storage provider has full atomicity. Instead, the abstraction exposes an explicit terminal commit boundary. SQLite now implements that boundary using a database transaction. Other providers must implement the same contract or remain on the conservative fallback path.

The lease predicate is evaluated inside the same transaction as the terminal mutation, reducing the race between external lease inspection and terminal persistence.

## Validation
- Structural C# brace balance: PASS.
- Source architecture checks: PASS by static inspection.
- Native .NET/SQLite execution: unavailable in current environment because `dotnet` is not installed.
- Android/Windows device execution: not performed.
- Live trading/order authority: none.
- Parallel research: remains disabled.

## Known limitation
The current v20.44 source-level fault harness is prepared to exercise transaction boundaries, but actual native SQLite fault execution still requires a .NET-capable runtime environment.

## Next build
v20.45: transactional terminal fault-injection execution harness and provider-neutral terminal commit validation, once a native .NET runtime is available; otherwise continue strengthening source-level contracts without claiming runtime execution.

## User Contributions — Deferred Until Needed
No user contribution is required for v20.44. NT8 samples and market-data packages remain deferred until the native data bridge/triangulation phase.
