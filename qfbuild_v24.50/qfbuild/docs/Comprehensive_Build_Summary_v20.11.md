# QuantForge — Comprehensive Build Summary v20.11

## Release identity
- Product: QuantForge Futures Research Platform
- Release: **v20.11**
- Baseline: v20.10 Durable Optimization / Robustness / Walk-Forward
- Focus: **Native first-line strategy acquisition, quarantine, source scrubbing, feature inventory, user component selection, and canonical strategy model lineage**
- Visual refinement: frozen by standing project direction
- PDF analysis: deferred by standing project direction

## Objective
Move the previously established browser-era script safety concept into the native .NET architecture without granting imported source execution authority merely because a source file was acquired.

The new boundary is:

SOURCE → ACQUISITION → QUARANTINE → STATIC AUDIT → FEATURE INVENTORY → USER SELECTION → GOVERNANCE REVIEW FOR SENSITIVE FEATURES → CANONICAL RESEARCH MODEL

There is deliberately no source compilation, loading, invocation, or execution in this layer.

## Implemented
1. Added native `StrategySourceArtifact`, language/status, feature-category, audit-report, component-selection, and canonical-model contracts.
2. Added `StrategySourceScrubber` with bounded regex/static inspection for indicators, signals, orders, risk rules, plots/drawings, alerts, timeframes, dependencies, network, filesystem, environment/secret access, dynamic execution, child processes/shells, authentication/credential indicators, and external data access.
3. Every detected feature receives an auditable feature ID and contextual source evidence.
4. Safety-sensitive features are explicitly marked.
5. Audit reports declare `ExecutionAllowed=false`.
6. Added explicit component-selection manifest/fingerprint. A selection cannot reference an undetected feature.
7. Safety-sensitive features cannot cross the canonical boundary through component selection alone; they require governance review.
8. Added `StrategyAcquisitionService` facade making quarantine explicit and separating acquisition/audit from canonical model construction.
9. Added canonical strategy model with source fingerprint and model fingerprint.
10. Canonical model is research-eligible but `LiveDeploymentEligible=false` by construction.

## Engineering rationale
### Acquisition is not execution
Imported text is treated as untrusted content. The acquisition service stores it conceptually as quarantined source and immediately performs static inspection. It never invokes a runtime loader.

### Detection is not proof
A detected network call is a safety signal, not proof of maliciousness. Absence of a signal is not proof of safety. Static scanning cannot certify obfuscated or semantically complex code.

### User selection is a pre-edit boundary
The audit produces feature IDs before canonical commitment. This supports the requested workflow in which the user can deselect components before a source strategy becomes part of the governed research representation.

### Canonical model prevents indicator/strategy drift
The canonical model is the future single source for indicator generation and strategy generation. Both can inherit the same source lineage and model fingerprint rather than being independently reinterpreted.

## Feature categories
- Indicators
- Signals
- Orders
- Risk management
- Plots and drawings
- Alerts
- Timeframes
- External dependencies
- Network access
- File-system access
- Environment access
- Dynamic execution
- Process execution
- Authentication/credential access
- External data access

## Safety boundary
The following remain prohibited:
- live orders
- live trading
- arbitrary code execution
- canonical market-data mutation
- undeclared application mutation
- manifest mutation

This release introduces no new trading authority.

## Data-fidelity boundary
This release does not create market-data fidelity. Imported OHLCV remains OHLCV. The scrubber does not infer tick, bid/ask, spread, or replay fidelity from source code.

## Validation
`docs/validate-v20.11.py` reports:
- 36 C# files
- C# structural balance: PASS
- strategy source artifact contract: PASS
- feature category inventory: PASS
- safety-sensitive selection gate: PASS
- no source execution: PASS
- dynamic execution detection: PASS
- network detection: PASS
- filesystem detection: PASS
- credential detection: PASS
- explicit component selection: PASS
- canonical model lineage: PASS
- live deployment remains disabled: PASS
- quarantine status: PASS
- acquisition facade: PASS
- Governance → Core dependency: PASS

Native compilation/device validation is not claimed because the current environment does not expose the required .NET/MAUI, Android, and Windows SDK toolchain.

## Known limitations
- Static detection is heuristic and not malware certification.
- No AST/compiler-backed semantic analysis yet.
- No archive/dependency graph resolver yet.
- No isolated native sandbox for executing approved subsets.
- No persistent strategy-source/quarantine repository table yet; this release establishes the governed service/contracts boundary first.
- No full Pine/NinjaScript semantic parser yet.
- No automatic indicator/strategy code generation yet; canonical model is the controlled intermediate representation foundation.
- No live deployment authority.

## Next build
v20.12 should add:
1. persistent strategy source/quarantine/audit records in SQLite;
2. immutable source artifact storage and lineage;
3. deterministic feature IDs independent of detection order;
4. language-aware token/AST adapters where available;
5. dependency/license records;
6. user-facing component-selection manifest persistence;
7. canonical model persistence;
8. canonical-model-to-indicator and canonical-model-to-backtest adapter foundations;
9. audit/evidence receipts linking source → selection → model → research.

## Completion status
v20.11 is complete as a **source-level native governance boundary**. It is not represented as a production malware scanner, compiler, sandbox, or live-trading system.
