# QuantForge User Manual v20.11

## Strategy Acquisition & Script Safety

### Purpose
The native strategy acquisition layer lets QuantForge inspect an imported strategy before it can become a governed research artifact.

### Safety model
Importing a script does **not** execute it.

The workflow is:
1. Acquire source.
2. Quarantine source.
3. Run first-line static audit.
4. Review detected feature categories.
5. Select permitted components.
6. Send safety-sensitive components to governance review rather than automatically committing them.
7. Build a canonical research model only after explicit selection/commit.

### What the scrubber looks for
The current first-line scanner can flag:
- indicators such as EMA/SMA/ATR
- crossover signals
- order operations
- stop/target operations
- plots/drawings
- alerts
- secondary timeframe operations
- imports/includes/requires
- network access
- file-system access
- environment/secret access
- dynamic execution
- child processes and shell commands
- authentication/credential indicators
- external data access

### Interpreting an audit
A feature being detected means that the scanner found a matching source pattern. It does not prove the feature is malicious. A clean result does not prove the source is safe.

### Component selection
Each detected feature receives an ID. The selection object records:
- source ID
- selected feature IDs
- selection fingerprint
- explicit commit request

Selecting a feature not present in the audit is rejected.

Safety-sensitive features cannot be committed through ordinary selection alone.

### Canonical strategy model
The canonical model retains source lineage and a model fingerprint. It separates:
- indicators
- entry rules
- exit rules
- risk rules
- timeframe rules
- visual elements
- alerts

The model is research-eligible but is not live-deployment eligible in v20.11.

### Fidelity warning
The scrubber does not improve market-data fidelity. Source code mentioning tick, bid/ask, replay, or other execution concepts does not create those data sources.

### Current limitations
This is a first-line static safety layer, not a malware certification system or execution sandbox. Full semantic parsing, dependency resolution, persistent quarantine storage, and code-generation adapters remain future work.

## Other QuantForge research functions
v20.11 retains the prior research stack:
- canonical data validation
- causal multi-timeframe processing
- deterministic backtesting
- optimization
- walk-forward analysis
- robustness analysis
- Monte Carlo/bootstrap analysis
- durable checkpoints and recovery
- evidence-linked research records

All existing live-trading prohibitions remain active.

## Build rule
Every QuantForge build includes an updated User Manual, New-Chat Handoff, Comprehensive Build Summary, release metadata, validation, safety boundaries, fidelity boundaries, limitations, and next steps.
