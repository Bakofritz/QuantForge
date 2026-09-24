# QuantForge v20.33 — Durable Checkpoint-Bound Case Continuation

## Purpose
Bind validated recovery checkpoints to an explicit, caller-supplied continuation step function without inventing or silently replaying research instructions.

## Implemented
- CheckpointBoundContinuationService
- explicit continuation cursor (sequence/cursor/state hash)
- checkpoint persistence after every accepted continuation step
- cursor-regression rejection
- required state-hash gate
- lease renewal on each continuation step
- bounded maximum continuation steps
- durable job completion only after a completed continuation step
- cancellation/failure terminal handling
- completion evidence/event recording
- ResearchRuntime integration
- architecture checks

## Safety boundaries
- No live trading/order authority.
- No arbitrary automatic restart.
- Recovery first passes existing fingerprint/lease/receipt/cancellation gates.
- Caller supplies the deterministic research operation for each cursor.
- Parallel execution remains disabled.

## Validation
- Source structural balance required.
- Native .NET/Android/Windows runtime compilation unavailable in this environment; no device validation claimed.

## Deferred user contributions
No contribution required for v20.33. NT8/MES/MNQ/strategy/replay/latency samples remain deferred until their specific validation phases.
