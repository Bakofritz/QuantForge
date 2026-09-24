# QuantForge v20.42 — Durable Native Fault-Result Reconciliation + Failure-Recovery Invariants

## Objective
Advance v20.41 from “the real SQLite operation can be deliberately failed” to “the failed operation is demonstrably contained, does not create false durable success, and can be retried through a controlled path.”

## Implemented
- Added `StorageFaultRecoveryCheck` and `StorageFaultRecoveryValidationResult` contracts.
- Added `SqliteStorageFaultExecutionAdapter.RunRecoveryInvariantAsync`.
- Each of the seven native SQLite fault points is exercised with the real injected storage seam.
- After the injected failure, the same logical operation is retried through a clean `SqliteLocalStore` instance.
- Retry success is verified against durable SQLite state.
- Lease acquisition and renewal, checkpoint, execution receipt, evidence, validation-result, and job-state persistence are covered.
- Added `StorageFaultRecoveryValidationService`.
- Added `ResearchRuntime.ValidateNativeStorageFaultRecoveryAsync`.
- Recovery validation results are attached to the existing evidence/event chain.
- Added architecture checks for the recovery-invariant adapter, service, seven fault points, and fail-closed contract.
- Corrected an existing source-level defect in `SqliteLocalStore.SaveDatasetAsync` where a lease-only `renewOnly` variable had been incorrectly referenced. Lease fault selection remains confined to `MutateLeaseAsync`.

## Recovery invariant
For each fault point:

`Inject failure → require explicit injected-fault observation → perform controlled retry → verify durable result`

A successful retry is not treated as proof that the original failed operation succeeded. The validation requires the original fault to be observed first and the later retry to establish the expected durable state.

## Engineering decisions
1. Fault injection remains opt-in through the internal storage constructor.
2. Normal production storage construction does not configure a fault injector.
3. Recovery validation uses a separate clean SQLite store instance after the injected failure, representing a new controlled attempt rather than continuing an interrupted storage call.
4. The validation does not automatically resume arbitrary research work; it validates storage invariants only.
5. Parallel research remains disabled until native concurrency evidence is available.

## Validation
- C# structural balance: PASS.
- Source-level architecture checks: PASS.
- Native fault/retry adapter implemented: PASS.
- Native .NET execution in current environment: NOT AVAILABLE.
- Actual native SQLite fault/retry execution in current environment: NOT EXECUTED / NOT CLAIMED.

## Safety boundary
- No live order authority.
- No live trading.
- No unrestricted AI mutation authority.
- No canonical market-data mutation authority added.
- Fault injection is validation-only infrastructure.

## Next engineering phase
v20.43 should connect these storage invariants to the durable worker/lease/recovery state machine and prove that storage failures during recovery do not produce invalid job-state transitions, duplicate terminal receipts, or silent lease ownership changes.

## Deferred User Contributions
No user contribution is required for v20.42. NT8/native replay/export samples, market-data packages, strategy sources, and measured latency observations remain deferred until their dedicated validation stages.
