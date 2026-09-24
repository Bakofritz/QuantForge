# QuantForge New-Chat Handoff — v20.75

Continue from QuantForge Master Build v20.75.

The recovery/reconciliation foundation through v20.65 remains intact. v20.70 established native qualification and resource admission. v20.75 consolidates v20.71-v20.75 into a storage-agnostic controlled-concurrency lifecycle state machine.

Current lifecycle:
Pending -> Admitted -> Running -> Completed/Failed/Canceled.
Admitted/Running may enter CancelRequested. Terminal aggregation requires all cases terminal and rejects duplicate ContextIds. Aggregate fingerprints are deterministic.

Parallel execution is NOT enabled by this milestone. Real lease acquisition, SQLite persistence, Android execution, and user-code invocation remain outside this state-machine layer and require their own qualified integration tests.

Next recommended consolidated milestone: integrate this lifecycle with the existing MultiCaseExecutionCoordinator, lease inspection/renewal, terminal commit store, and resource receipts behind a fail-closed integration adapter; then run the combined contention/fault matrix before enabling any parallel scheduling.

Standing documentation rule remains: every build package includes a Comprehensive Build Summary, detailed User Manual, New-Chat Handoff, Validation Result, Release Manifest, and Source Hashes.
