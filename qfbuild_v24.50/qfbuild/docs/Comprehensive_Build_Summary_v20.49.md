# QuantForge v20.49 — Recovery Preflight Checkpoint + Lease Binding

## Purpose
Bind the durable recovery preflight decision to the exact checkpoint and accepted execution lease used by a controlled recovery continuation.

## Changes
- Extended `RecoveryPreflightReceipt` with checkpoint sequence/cursor/state hash and accepted lease version/fingerprint.
- Added deterministic accepted-lease fingerprinting.
- Updated SQLite persistence, reads, immutable conflict detection, and schema migration for the new fields.
- RecoveryContinuationService now records blocked preflight decisions and, for successful recovery preparation, records an Allowed preflight receipt only after checkpoint validation and fresh lease acquisition.
- Missing accepted lease after a Ready state is fail-closed.
- Added architecture validation for binding fields and integration.

## Safety
The preflight remains read-only with respect to research content. It does not invent checkpoints or repair ambiguous history. A Ready decision is bound to the checkpoint actually validated and lease actually acquired.

## Validation limitations
The environment lacks the .NET SDK/runtime. Native SQLite execution, Android execution, Windows execution, and device-level integration are not claimed.
