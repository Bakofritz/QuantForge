# QuantForge User Manual v20.48

## Recovery preflight record
Before QuantForge tries to continue a research job after a worker interruption, it can save a durable record of the recovery decision.

The record tells you:
- which batch, context, and job were involved;
- which worker/device requested recovery;
- whether recovery was Allowed or Blocked;
- why that decision was made;
- which integrity check produced the decision;
- whether a terminal result was already known.

## What this does not do
The preflight record does not restart a job, acquire a lease, repair history, or create a missing result. It is an evidence record of the decision.

Even when the preflight says Allowed, the later continuation process still checks the checkpoint and lease before continuing.

## Inspecting history
The runtime exposes `LoadRecoveryPreflightsAsync` so the application can review the saved recovery decisions for a job.

## Current limitations
Native execution validation requires a .NET/SQLite-capable environment. Parallel execution and live trading remain disabled.
