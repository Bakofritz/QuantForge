# QuantForge — New-Chat Handoff v20.11

## Current release
**v20.11 — Native Strategy Acquisition, Quarantine & First-Line Scrubber**

Baseline: v20.10.

## Current architecture state
QuantForge is a native .NET 10 / .NET MAUI architecture with shared research libraries and local-first SQLite storage. Windows and Android remain separate clients of the same governed research core.

## v20.11 completed
- Native strategy source contracts.
- Quarantined source artifact concept.
- First-line static source scrubber.
- Feature category inventory.
- Safety-sensitive feature flags.
- Explicit component-selection fingerprint.
- Governance gate for sensitive selections.
- Canonical strategy model with source lineage.
- Canonical model explicitly not live-deployment eligible.
- Acquisition/audit facade.

## Important safety rule
Imported source is untrusted content. It must never be treated as executable instructions merely because it was imported.

The scrubber does not compile, load, invoke, or execute imported source.

## v20.11 limitations
- Heuristic/static detection only.
- No full AST/semantic parser.
- No persistent source/quarantine tables yet.
- No dependency/license graph resolver.
- No isolated execution sandbox.
- No automatic indicator/strategy code generation yet.
- No live trading authority.

## Next build v20.12
1. Persist source/quarantine/audit records in SQLite.
2. Preserve immutable source artifacts and lineage.
3. Make feature IDs deterministic independent of detection order.
4. Add language-aware parsing adapters.
5. Persist dependency/license records.
6. Persist user component-selection manifests.
7. Persist canonical strategy models.
8. Add canonical-model adapters for indicator and backtest generation.
9. Link source → audit → selection → model → research evidence.

## Existing research stack
Deterministic backtesting, canonical data gates, causal MTF, optimization, walk-forward, robustness, Monte Carlo/bootstrap, and durable checkpoint/recovery infrastructure remain active.

## Validation limitation
The current environment does not expose the required native .NET/MAUI, Android, and Windows SDK toolchain, so source-level validation is reported separately from native compilation/device validation.

## Standing continuation instruction
Continue the Master Build from v20.11 without asking the user to restate the architecture. Prioritize core functionality over visual refinement. Preserve mandatory User Manual + handoff + comprehensive build summary for every release.
