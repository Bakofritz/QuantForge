# QuantForge — New-Chat Handoff v20.29

## Current release
**v20.29 — Resource Governance + Lease Heartbeat + Execution Receipts**

Baseline: v20.28.

## Completed
1. Added `MultiCaseResourcePolicy` with explicit case/batch duration and heartbeat controls.
2. Added case lease heartbeat renewal through `IJobLeaseStore.RenewAsync`.
3. Added bounded case cancellation on resource timeout.
4. Added deterministic execution-receipt fingerprints.
5. Added receipt evidence/event recording through the existing append-only evidence boundary.
6. Integrated the resource policy into `ResearchRuntime.ExecuteMultiCaseAsync`.
7. Preserved the explicit single-concurrency gate.

## Important architecture rule
Resource governance is an orchestration boundary, not a claim of OS-level CPU/RAM enforcement. Parallel execution remains disabled until native runtime/resource validation exists.

## Recovery rule
A lease renewal failure must stop the case rather than permit execution after the lease authority expires.

## Safety boundary
Research only. No live orders, live trading, arbitrary imported-code execution, canonical-data mutation, or unrestricted AI mutation.

## Validation limitation
Native .NET compilation, Windows packaging, Android packaging, and device execution remain unavailable in the current environment.

## Next build
**v20.30 — Durable Resource Receipts + Batch Accounting + Worker Identity**

Continue the Master Build without asking the user to restate the architecture.
