# QuantForge Master Build v21.05

## Consolidated scope
This build consolidates the Strategy Import / Audit / Feature Scrubbing / Governance Review milestones that follow v21.00.

## Major additions
- Explicit governance decision records for audited strategy sources.
- Separation of static audit from execution authority.
- Explicit component-selection binding.
- Read-only research capability authority.
- Final canonical research commit gate.
- Immutable fingerprints binding source audit, component selection, governance decision, and canonical model.
- Research commit receipt that cannot grant live-deployment authority.
- Architecture checks for rejected decisions, authority separation, and receipt binding.

## Security and safety boundary
Imported source remains quarantined and is never compiled, loaded, or executed by the scrubber. Static detection is heuristic and is not a malware certification. Safety-sensitive features require explicit governance review. The research commit gate grants only read-only research authority.

## Validation
- Structural C# validation: PASS.
- Governance decision contract: PASS.
- Rejected decision blocked at canonical commit: PASS.
- Audit does not grant execution authority: PASS.
- Receipt bound to selection/model fingerprints: PASS.
- Live deployment eligibility remains false: PASS.
- Native .NET execution: NOT CLAIMED; SDK unavailable in build environment.
- Native SQLite execution: NOT CLAIMED.
- Android/device execution: NOT CLAIMED.
- Live trading/order authority: DISABLED.
- Arbitrary imported-code execution: DISABLED.

## Documentation
Includes the detailed User Manual, New-Chat Handoff, Validation Result, Release Manifest, Source Hashes, and build-count information.
