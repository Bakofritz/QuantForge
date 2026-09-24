# QuantForge v20.65 — Consolidated Recovery Reconciliation Build

## Scope
This release consolidates the planned v20.61, v20.62, v20.63, v20.64 and v20.65 recovery milestones into one build effort.

## v20.61 — Immutable Resolution Decision Boundary
Introduces evidence-bound resolution records for Prepared-only operations. Resolution records are durable control/audit artifacts and are not execution commands.

## v20.62 — Authorization Binding and Stale-Decision Protection
A resolution is bound to the operation fingerprint, reconciliation decision fingerprint, evidence fingerprint, resolver identity, expiration window and consumption state. Stale, expired or reused resolutions are rejected.

## v20.63 — External-Side-Effect Evidence Boundary
Introduces a dedicated evidence contract for externally completed or rejected side effects. Evidence remains informational/control-bound and cannot independently authorize callback execution.

## v20.64 — Adversarial Reconciliation Matrix
The consolidated harness covers ambiguous Prepared-only state, conflicting resolution, duplicate resolution, stale decision, expiry, and automatic-replay prohibition.

## v20.65 — Integration Gate
The consolidated validation harness requires all recovery reconciliation boundaries to remain present before this section can be considered structurally complete.

## Core Safety Principle
A durable reconciliation record is not equivalent to permission to execute code. External completion evidence is not equivalent to permission to replay code. An ambiguous Prepared-only operation remains fail-closed.

## Runtime Qualification
Native .NET execution is unavailable in the current build environment, so runtime success is not claimed. SQLite and Android/device execution remain separate gates.

## Documentation
This package includes this summary, a detailed user manual, a new-chat handoff, validation results, a release manifest and source hashes.
