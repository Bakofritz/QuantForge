# QuantForge v20.20 — Native NinjaTrader Data Validation Layer

## Purpose
Add a governed read-only NinjaTrader 8 data bridge and a cross-validation architecture that avoids requiring multi-GB tick archives merely to validate an importer.

## Key decision
A small number of complete representative tick-export sessions can validate parser/normalization fidelity for the compared periods, while long Minute/Day exports validate aggregation, continuity, timestamps, sessions, contract coverage, and decade-scale archive consistency. This does not prove untested tick history or Level II fidelity.

## Implemented
- NT8 source inventory with per-file SHA-256 and immutable inventory fingerprint.
- Native Replay/Historical source classification hooks.
- Native extractor interface so a version-specific/native parser can be attached without changing canonical research contracts.
- NT8 text export importer for Tick/Minute/Day forms supported by the canonical parser.
- Tick-to-minute deterministic aggregation.
- Cross-validation comparing timestamps, OHLC, volume, tick-size conformity, and fingerprints.
- Validation-plan generator selecting representative tick sessions plus full-range Minute and Day samples.
- Explicit distinction between native-source extraction and exported-text reference data.

## Validation sampling policy
Recommended initial validation package:
- 3–5 complete tick sessions per instrument/contract family.
- Include an ordinary session, overnight/session-boundary coverage, a high-volatility/news session, and preferably a contract-roll-adjacent session.
- 1–2 weeks of tick exports is stronger than the minimum if storage is practical; a few full days is sufficient for an initial parser validation gate.
- Up to a decade of Minute and Day exports can be used as lightweight longitudinal integrity checks.
- Do not require decade-scale tick data unless the research question specifically requires decade-scale tick fidelity.

## Important fidelity boundary
NinjaTrader documents that Market Replay uses historical tick data, while Tick Replay replays Last events and provides the best inside bid/ask associated with those Last events; separate historical Bid/Ask series are needed for historical bid/ask events. NinjaTrader also notes that generated Minute/Day bars may differ from provider-supplied bars because timestamp granularity can differ. These distinctions are preserved in QuantForge's validation model.

## Current limitation
No actual NT8 native binary/replay sample was present in the build workspace, so v20.20 does not claim successful decoding of a proprietary native replay file. The architecture is ready for a real native sample and will not silently treat an unknown binary format as valid data.

## Safety
- Read-only source intent.
- No writes to NinjaTrader directories.
- Raw source fingerprints retained.
- Imported data must pass canonical validation before research use.
- Export comparison validates only fields actually compared.
