# QuantForge v20.21 — NT8 Triangulated Data Validation

## Build purpose
Strengthen the NT8 data bridge with an independent, multi-layer validation system that can validate a small representative tick sample against NT8 exported tick data while using longer Minute and Day exports for longitudinal coverage validation.

## Implemented
- Direct native/replay tick-vs-export tick comparison.
- Minute native/derived-vs-export comparison.
- Day native/derived-vs-export comparison.
- Record-count coverage layer.
- Chronology, duplicate timestamp, OHLC, volume, and tick-size integrity layer.
- Deterministic validation fingerprints.
- NT8 export importer now rejects non-chronological and duplicate timestamps instead of silently accepting them.
- Explicit fidelity boundary: agreement validates only compared fields/ranges.
- Small-sample validation remains the default; no requirement for multi-GB tick archives.

## Recommended validation corpus
- Tick: 3–5 complete representative sessions initially; 7–14 days is optional for additional confidence.
- Minute: as much historical range as practical, including long-range coverage.
- Day: full available history when inexpensive, potentially a decade.
- A matching native replay + exported tick sample is preferred when native replay parsing is ready.

## Architecture
NT8 native source -> raw/source inventory -> extractor -> normalized events -> independent export comparison -> integrity checks -> canonical dataset eligibility.

Exports are reference representations, not automatically authoritative truth. QuantForge preserves source identity and comparison results.

## Validation
- 57 C# files.
- Lexical structural balance: PASS.
- NT8 contracts/bridge/importer/validation engine: PASS.
- Direct tick comparison: PASS.
- Minute comparison: PASS.
- Day comparison: PASS.
- Coverage layer: PASS.
- Integrity layer: PASS.
- Deterministic validation fingerprint: PASS.
- No live authority: PASS.
- Native compilation/device runtime: NOT AVAILABLE in current environment.

## Fidelity limitations
No claim is made that a native NT8 file contains Bid/Ask, Level II, or complete replay-event information until the actual source is inspected. No parser is considered validated merely because a file is located in an NT8 directory.

## Deferred user contributions
No contribution is required to complete this build.

Later, when the appropriate phase is reached:
- **NT8 REPLAY** — small native Market Replay sample for native-format forensics.
- **NT8 TICK** — matching NT8 exported tick TXT for direct comparison.
- **NT8 MINUTE** — long-range Minute export for continuity/aggregation validation.
- **NT8 DAILY** — long-range Day export for longitudinal coverage.
- **REPLAY MATCH** — native replay + matching export pair.

Do not collect multi-GB tick archives unless a later validation result demonstrates that additional coverage is necessary.

## Next build
v20.22: native NT8 sample forensics and parser implementation against an actual small native sample, with strict quarantine and no write access to NinjaTrader data.
