# QuantForge New-Chat Handoff v20.20

Current baseline: v20.20.

Focus completed: governed NinjaTrader 8 data bridge foundation and lightweight cross-validation strategy.

Continue next with a real NT8 native sample when available. First inspect and fingerprint the sample without modifying it. Implement/version the native replay extractor only after its actual file structure is established. Compare the extracted sample to matching NT8 Tick export text, then validate Minute and Day continuity over long ranges. Preserve source contract identity, timezone, timestamps, event ordering, bid/ask availability, and all fidelity boundaries.

Recommended validation corpus: 3–5 complete tick sessions initially; preferably include ordinary, overnight/session-boundary, high-volatility/news, and roll-adjacent conditions. Use long Minute and Day exports for longitudinal validation rather than downloading years of ticks.

Do not claim native replay decoding until an actual native sample has passed parser and cross-validation tests.

Visual refinement remains deferred. Every build continues to include User Manual, Comprehensive Build Summary, New-Chat Handoff, Release Manifest, Validation Result, Source Hashes, limitations, and fidelity declaration.
