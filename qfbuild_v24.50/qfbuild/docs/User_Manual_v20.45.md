# QuantForge v20.45 User Manual

## What this build does
This build tests whether QuantForge can safely handle a failure while saving the final result of a research case.

A terminal result means the research case is finished as one of:
- Completed
- Failed
- Canceled

The system saves several pieces of proof together: the job status, execution receipt, evidence record, and research event.

## Transactional fault validation
The runtime exposes:
`ValidateTransactionalTerminalFaultAsync(databasePath)`

This is a validation function only. It does not run a trading strategy against a broker and cannot submit an order.

### What the test does
For each fault point it:
1. creates a test research job;
2. creates a temporary execution lease;
3. deliberately injects one controlled storage failure;
4. checks that the transaction fails;
5. checks that the failed transaction did not leave a half-completed result;
6. repeats the same commit without the injected failure;
7. checks that the complete terminal record now exists.

## Fault locations
- Job state write
- Receipt write
- Evidence write
- Event write
- Immediately before commit

## Interpreting results
**PASS** means the tested fault was contained, the transaction rolled back, a clean retry committed, and all expected terminal records were verified.

**FAIL** means the expected safety behavior could not be demonstrated. A failure must not be treated as a successful research result.

## Current validation limitation
The present environment does not have the .NET runtime, so native SQLite execution has not yet been performed. The feature is implemented and structurally validated, but native execution remains an explicit pending validation step.

## Related concepts
- **Lease:** temporary ownership of a research job by one worker.
- **Receipt:** durable record describing what happened.
- **Checkpoint:** saved progress that can support controlled recovery.
- **Transaction:** a group of database changes that either all become permanent or all roll back.

## Deferred user contributions
No user files are needed for this build. NT8 and market-data samples remain deferred until the native data-validation stage.
