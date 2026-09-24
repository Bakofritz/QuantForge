# QuantForge v21.00 User Manual

## Purpose
v21.00 provides the governed research-batch integration layer for read-only backtesting and deterministic optimization.

## Operating model
1. Create an immutable `ResearchBatchManifest` containing the approved research contexts.
2. Validate that the manifest fingerprint represents the intended batch.
3. Require a qualified native runtime.
4. Supply a resource policy and memory budget.
5. Run only with `ReadOnlyResearchAuthority=true`.
6. QuantForge reloads canonical datasets and verifies dataset fingerprints, instrument, and timeframe.
7. The deterministic optimizer evaluates the declared parameter ranges.
8. Results are deterministically ordered and fingerprinted.
9. Existing terminal receipt/checkpoint infrastructure remains responsible for durable execution state.

## Safety rules
- A manifest does not grant live trading authority.
- Optimization does not grant arbitrary imported-code execution authority.
- A dataset fingerprint mismatch stops the operation.
- An unqualified runtime is denied.
- Resource admission failure is denied.
- Cancellation is propagated; callers should treat cancellation as a terminal control event rather than silently retrying.

## Current limitations
Native runtime execution, native SQLite execution, and Android/device execution are not claimed by this package. Structural validation is the available validation mode in this environment.
