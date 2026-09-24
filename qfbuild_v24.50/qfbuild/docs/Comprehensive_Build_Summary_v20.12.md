# QuantForge v20.12 — Persistent Strategy Governance & Canonical Model Lineage

## Baseline
v20.11 — Strategy Scrubber & Quarantine.

## Purpose
v20.12 makes the strategy-acquisition safety boundary durable in the local SQLite evidence architecture. Imported source remains inert and quarantined; audit, component selection, canonical model, and lineage are persisted without granting execution or live-trading authority.

## Implemented
- Immutable quarantined source storage with source-content fingerprint binding.
- Persisted static audit report, including safety signals, dependencies, and license-related signals.
- Deterministic feature identifiers derived from feature semantics/evidence rather than detection order.
- Persistent component-selection manifest with immutable selection fingerprint.
- Persistent canonical strategy model with immutable model/source lineage.
- Append-only strategy lineage events: acquisition → audit → selection → canonical model.
- Fingerprint validation prevents selection drift between audit and canonicalization.
- Canonical research model remains `LiveDeploymentEligible=false`.
- No imported source is compiled, loaded, invoked, or executed by the scrubber/governance layer.

## Architecture
`Source → Quarantine → Audit → User Selection → Canonical Model → Research`

The persistent store is local-first SQLite. Strategy source text is stored as quarantined data; it is not treated as trusted instructions.

## Validation
Source-level validation only in this environment. No .NET SDK/compiler, Android SDK, or Windows SDK is available, so native compilation/device validation is not claimed.

## Data/safety boundary
This release does not add live orders, live trading, arbitrary code execution, canonical market-data mutation, or unrestricted AI authority. Static detection is heuristic and is not malware certification.

## Next
v20.13 should connect canonical strategy models to the research engine through a governed adapter: indicator calculation/visual-signal representation, deterministic backtest translation for supported model constructs, explicit unsupported-feature reporting, and multi-script isolation.
