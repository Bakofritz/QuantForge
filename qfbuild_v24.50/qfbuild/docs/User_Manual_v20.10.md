# QuantForge User Manual v20.10

## What changed
v20.10 extends durable research execution to optimization, parameter robustness, and chronological walk-forward analysis.

## Research operations

### Optimization
Create an optimization job with fast/slow parameter ranges. QuantForge constructs the deterministic case set, executes cases in bounded chunks, and saves the completed case results after each chunk.

If the application stops, the same job can resume only when the dataset, configuration, engine identity, and total case count still match.

### Robustness
Run local perturbations around a declared baseline. Each perturbation is an independent case. The baseline is not silently changed during the perturbation run.

### Walk-forward
Define training bars, test bars, and step size. Each window optimizes only on its training slice and evaluates the selected parameters on the subsequent test slice. Completed windows are persisted before the cursor advances.

## Recovery behavior
For all three operations:
1. acquire the job lease;
2. validate canonical dataset identity;
3. validate job fingerprints;
4. load and verify persisted progress if present;
5. check cancellation;
6. renew the execution lease;
7. execute one bounded unit;
8. persist aggregation state and checkpoint;
9. repeat;
10. emit final evidence only after all work completes.

A mismatched or corrupted checkpoint is rejected rather than guessed or repaired silently.

## Multi-device behavior
Each device uses its own local storage. A logical job is protected by an execution lease so two devices do not simultaneously own the same mutable research execution state.

Immutable evidence can be synchronized later without sharing a physical SQLite database file over a network filesystem.

## Evidence
Final evidence identifiers are deterministic by operation and job. Existing evidence is not duplicated. Completion events are idempotent.

## Safety
- Research operations have no live trading authority.
- Imported source is not executed merely because it is present in a research workspace.
- Internet-discovered content remains subject to quarantine and safety inspection.
- AI analysis remains non-authoritative and cannot silently change application policy, canonical data, or trading permissions.

## Data fidelity
Current native research is OHLCV bar-driven. Results must not be interpreted as having bid/ask, tick-order, spread, replay, or intrabar execution fidelity unless a corresponding source and execution model are explicitly integrated.

## Current limitations
The current environment has not compiled the MAUI solution because the required native toolchain is unavailable. Visual refinement is intentionally postponed until core runtime functionality is operational. PDF analysis is deferred.

## Next major capability
The next planned phase is native Strategy/Indicator canonicalization plus the first-line Script Scrubber/Quarantine. Imported scripts will be inspected before any governed commit and the user will be able to select which discovered components are permitted into the canonical research representation.
