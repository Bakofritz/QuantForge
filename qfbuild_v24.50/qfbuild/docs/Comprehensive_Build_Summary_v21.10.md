# QuantForge Master Build v21.10 — Consolidated Strategy Lifecycle Governance

## Scope
Consolidates the strategy-lifecycle milestones following v21.05: persistent provenance, canonical lifecycle binding, least-privilege capability manifests, research registry admission, and source-aware governed import profiles.

## Security model
Imported source remains quarantined. Static inspection does not compile or execute source. A research capability manifest grants only declared research capabilities and explicitly denies live-order submission. Registry admission requires a contiguous fingerprint chain from source through audit, selection, review, commit, canonical model, provenance, and capability manifest.

## Supported import profiles
NinjaScript, TradingView Pine, C#, Python, and JavaScript may enter the static audit facade. This build does not claim semantic parity across languages and does not execute imported source.

## Explicit non-goals
No live order authority, no arbitrary imported-code execution, no credential use, no external network execution, and no automatic promotion from research registry to live deployment.
