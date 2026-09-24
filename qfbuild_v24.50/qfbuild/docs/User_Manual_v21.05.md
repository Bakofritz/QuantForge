# QuantForge v21.05 User Manual

## Strategy import safety flow
1. Acquire the source into quarantine.
2. Run the static first-line scrubber.
3. Review detected feature categories and evidence.
4. Select only components intended for canonical research representation.
5. Submit the selection for governance review when sensitive capabilities are present.
6. Record an explicit research-only governance decision.
7. Build the canonical research model.
8. Pass the final governance commit gate.
9. Use the resulting receipt as provenance for downstream read-only research.

## Important distinction
The scrubber identifies source patterns; it does not certify a strategy as safe. A successful audit does not mean the source may execute. A canonical research commit does not grant live trading authority.

## Research authority
The supported authority produced by this stage is `ReadOnlyResearch`. It is intended for backtesting, optimization, analysis, and other non-live research workflows.
