# QuantForge — New-Chat Handoff v20.9

## Current release
**v20.9 — Durable Monte Carlo Checkpoints / Resumable Research**

Baseline: v20.8.

## Completed
1. Added durable `ResearchProgress` state.
2. Added SQLite `research_progress` persistence.
3. Added deterministic Monte Carlo chunk execution and resumable serialized state.
4. Added persisted RNG state and sampled distribution state.
5. Added fingerprint-bound resume validation.
6. Added durable cancellation checks.
7. Added lease renewal before bounded chunks.
8. Added progress + checkpoint persistence after each chunk.
9. Preserved immutable dataset and evidence boundaries.
10. Preserved multi-device single-job lease isolation.

## Current limitations
- Resumable chunk execution is fully integrated into Monte Carlo first; optimization, robustness, and walk-forward require their own operation-specific durable aggregators.
- Native .NET/MAUI/Android/Windows compilation is still blocked by unavailable SDKs in the current environment.
- OHLCV remains the available data-fidelity boundary.
- Visual refinement remains frozen.
- PDF analysis remains deferred.

## Next build
v20.10: generalize durable chunk execution to optimization, robustness, and walk-forward; add fault-injection testing at persistence boundaries; reduce large distribution-state storage where safe; then proceed to canonical strategy/indicator modeling and native script scrubber/quarantine integration.

Continue the Master Build without asking the user to restate the architecture.
