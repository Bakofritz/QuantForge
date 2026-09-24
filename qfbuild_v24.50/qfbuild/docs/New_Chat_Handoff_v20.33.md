# QuantForge v20.33 New-Chat Handoff

Resume from v20.33. The build adds durable checkpoint-bound continuation on top of v20.32 controlled recovery. A validated recovery checkpoint can now drive an explicit caller-supplied continuation step function. Each step renews the lease, rejects cursor regression, requires a state hash, persists a new checkpoint, and completes the job only after the continuation reports completion. Runtime integration is exposed through ResearchRuntime.ExecuteRecoveryContinuationAsync. Parallel execution remains disabled; native compilation/device validation remains pending.

Next logical phase: continuation result/receipt reconciliation and batch accounting integration, followed by native toolchain validation when available.
