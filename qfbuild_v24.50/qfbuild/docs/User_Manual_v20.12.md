# QuantForge User Manual v20.12

## Strategy acquisition
1. Acquire source into quarantine.
2. Run the first-line static audit.
3. Review detected categories, evidence, dependencies, safety signals, and license signals.
4. Select only the audited components intended for research.
5. Commit the selection.
6. Build the canonical research model.

## Persistence
Quarantine records, audits, selections, canonical models, and lineage events are persisted in local SQLite. Source IDs and fingerprints prevent silent replacement.

## Canonical model
The canonical model is a research representation, not executable source. It is research-eligible when successfully committed and remains ineligible for live deployment in v20.12.

## Safety
The scrubber does not compile, load, invoke, or execute imported source. Detection signals are evidence for review, not proof of maliciousness or safety. Network, filesystem, environment, dynamic execution, process, authentication, external dependency, and order-related features remain safety-sensitive.

## Research boundary
Canonicalization does not upgrade market-data fidelity. Current imported market data remains subject to the existing OHLCV fidelity declarations.
