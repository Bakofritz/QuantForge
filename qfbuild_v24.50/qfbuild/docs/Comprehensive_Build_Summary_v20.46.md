# QuantForge v20.46 — Transactional Terminal Duplicate-Result Protection

## Purpose
v20.46 hardens the transactional terminal commit introduced in v20.44/v20.45. A completed research result may be retried after an uncertain worker/network outcome, but the retry must not create a second terminal result for the same job. A different result for an already-terminal job is treated as an integrity conflict.

## Changes
- Added `TerminalCommitConflictException` with explicit conflict codes.
- Added `TerminalCommitRules` for terminal-state classification and exact receipt identity comparison.
- Added idempotency/conflict gate inside the SQLite terminal transaction.
- Identical terminal receipt retry is accepted as a no-op.
- Different terminal receipt/result for the same job is rejected.
- Non-terminal receipt states are rejected by terminal commit.
- Missing research job is rejected.
- A job already terminal with a different requested terminal state is rejected.
- Added `SqliteTerminalCommitIdempotencyAdapter`.
- Added `TerminalCommitIdempotencyValidationService` and runtime entry point.
- Added architecture checks.
- Preserved no-live-trading/no-order authority and sequential multi-case execution.

## Integrity model
`same durable identity + same receipt content => idempotent retry`

`same job + different terminal result => integrity conflict`

`terminal commit + non-terminal receipt => rejected`

## Native execution status
The environment used for this build does not provide the .NET SDK/runtime. The native SQLite adapter is implemented but was not executed here. No native compilation, Android runtime, or Windows runtime result is claimed.

## Validation
- Total C# files: 115
- Source C# files: 93
- Structural brace validation: PASS
- Idempotency contract checks: PASS
- Runtime entry-point checks: PASS
- Native .NET/SQLite execution: unavailable/not executed

## Safety
No live trading, order submission, unrestricted AI mutation, canonical data mutation, or undeclared source execution was added.

## Next build
v20.47 should extend duplicate-result protection into worker reconciliation/recovery and verify that duplicate terminal attempts cannot create duplicate batch accounting outcomes.
