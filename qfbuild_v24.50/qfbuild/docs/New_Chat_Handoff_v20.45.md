# QuantForge v20.45 — New Chat Handoff

## Current state
QuantForge is at v20.45. v20.44 established a SQLite transactional terminal commit boundary. v20.45 adds a dedicated fault-execution harness for that transaction.

## What v20.45 changed
The terminal transaction can now be deliberately interrupted at five points:
1. job-state write;
2. receipt write;
3. evidence write;
4. event write;
5. immediately before database commit.

For each fault, the intended validation sequence is:
`inject failure → require failure → verify rollback → clean retry → verify complete terminal outcome`.

## Important environment limitation
The current build environment does not contain the .NET SDK/runtime. Do not claim native execution, SQLite rollback execution, Android execution, or Windows execution until a suitable runtime environment is available.

## Next build
v20.46 should implement terminal commit idempotency and duplicate-outcome protection. The key invariant is:
`same logical terminal commit repeated = one durable outcome, not two`.
Conflicting content under an existing immutable receipt identity must fail closed.

## Safety
No live trading, no order submission, no unrestricted code execution, no autonomous policy mutation, and no canonical-data mutation authority.

## User Contributions — Deferred Until Needed
Do not request NT8 or market-data contributions yet. Use the established short codes only when the relevant data-bridge phase is reached.
