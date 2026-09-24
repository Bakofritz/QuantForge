# QuantForge New-Chat Handoff — v20.21

Current baseline: **v20.21 NT8 Triangulated Data Validation**.

Continue the Master Build autonomously under the standing directive. Visual refinement remains deferred while core functionality is prioritized.

## Current data architecture
NT8 source inventory -> native/export extraction -> normalization -> independent validation -> canonical immutable dataset -> causal research engine.

## v20.21 additions
- Direct tick comparison.
- Minute and Day cross-validation.
- Coverage and integrity layers.
- Deterministic validation fingerprints.
- Strict chronological/duplicate rejection in NT8 export importer.
- Small-sample validation strategy so large tick archives are not required by default.

## Next build: v20.22
Implement native NT8 sample forensics and parser work against an actual small native replay/historical sample when one becomes available. Keep unknown formats quarantined and read-only. Do not invent proprietary format details.

## Deferred user contributions
Do not ask for these before their workflow is reached:
- **NT8 REPLAY** — native replay sample.
- **NT8 TICK** — matching exported tick TXT.
- **NT8 MINUTE** — long-range minute TXT.
- **NT8 DAILY** — long-range day TXT.
- **REPLAY MATCH** — paired replay/export validation set.

## Mandatory per-build outputs
Every build must include the User Manual, New-Chat Handoff, Comprehensive Build Summary, release metadata, validation results, limitations/fidelity boundaries, source hashes, and exact artifact path/hash.
