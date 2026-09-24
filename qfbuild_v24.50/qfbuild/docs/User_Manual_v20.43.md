# QuantForge v20.43 User Manual

## Recovery-State Failure Integrity
v20.43 hardens the boundary between research execution and durable reporting.

### What the system now protects
1. A terminal receipt cannot be intentionally published before the corresponding terminal job state is durable.
2. Terminal mutations require a currently valid lease owned by the executing device when lease inspection is available.
3. Recovery is blocked when a durable cancellation request exists.
4. More than one terminal receipt for a case is treated as an integrity violation.
5. A completed job without exactly one completed terminal receipt is not accepted as an intact successful outcome.
6. A terminal receipt without a terminal job state is a disagreement, not proof of success.
7. The validator never repairs or resumes state automatically.

### Runtime validation
Use `ResearchRuntime.ValidateRecoveryStateIntegrityAsync(jobId, expectedWorkerDeviceId)` against a durable research job. A passing result means the inspected job, lease, cancellation, and terminal receipt records agree under the implemented checks. It is not a claim of full production/native validation until native runtime tests are executed.

### Safety boundary
QuantForge remains a research/backtesting system. v20.43 introduces no live trading or order authority and does not grant AI permission to mutate canonical state.
