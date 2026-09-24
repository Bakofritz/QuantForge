# QuantForge — New-Chat Handoff v20.28

## Current release
**v20.28 — Durable Multi-Case Checkpoint Orchestration**

Baseline: v20.27.

### Completed
- Durable multi-case progress using ResearchProgress.
- Batch-level ResearchCheckpoint persistence.
- Deterministic batch fingerprint binding.
- Progress-hash integrity verification.
- Terminal-case skip/recovery protection.
- Per-case lease preservation.
- Runtime integration through ExecuteMultiCaseAsync.

### Recovery model
A batch resumes by validating its immutable batch fingerprint and progress hash. Completed/failed/canceled cases are not rerun. An unresolved case resumes through its own case-level job/checkpoint path; v20.28 does not pretend that a partially executed case can resume from an arbitrary instruction point.

### Native validation limitation
The environment does not provide the .NET SDK/compiler, Android SDK/device, or Windows SDK/runtime. Therefore no native compilation, APK, Windows package, or device execution claim is made.

### Visual state
Visual refinement remains frozen until core runtime capability is substantially complete.

### Next build
v20.29 — Resource Governance + Lease Heartbeat + Execution Receipts for Multi-Case Research. Parallel scheduling remains gated on native runtime/resource validation.

### Safety
No live trading, order routing, arbitrary source execution, canonical-data mutation, or unrestricted AI mutation.

### Deferred User Contributions
No files are needed now. Use the established contribution codes when requested at the appropriate phase: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, NT8 CONFIG.
