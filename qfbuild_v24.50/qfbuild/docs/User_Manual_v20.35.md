# QuantForge User Manual — v20.35

## Batch Manifest Integrity
A batch manifest is the immutable declaration of which research cases belong to a batch and the fingerprints that identify their dataset, strategy, configuration, and engine.

### Validation
`ValidateBatchManifestAsync` compares the manifest against durable receipts and durable jobs.

It can detect:
- duplicate context or job IDs;
- incomplete case identity;
- receipts for undeclared contexts;
- receipt/job identity mismatch;
- dataset/configuration/engine fingerprint mismatch;
- completed jobs without terminal receipts;
- canceled/failed jobs without matching terminal receipts;
- multiple terminal receipts requiring reconciliation.

### Interpretation
`Valid` means no integrity discrepancy was detected by this validator. Other states identify the category of discrepancy. Validation does not modify the underlying records and does not grant execution authority.

## Recovery
A stale worker does not automatically resume work. Checkpoint, fingerprint, cancellation, lease, receipt, and manifest conditions must be satisfied before controlled continuation.

## Safety
Live trading and order submission are not available. Parallel multi-case execution remains disabled until native runtime/resource validation and a controlled concurrency review are completed.

## User Contributions — Deferred Until Needed
No contribution is needed for v20.35. Future code words remain: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, MES DATA, MNQ DATA, ROLL DATA, STRATEGY SRC, STRATEGY TEST, REPLAY MATCH, LATENCY DATA, NT8 CONFIG.
