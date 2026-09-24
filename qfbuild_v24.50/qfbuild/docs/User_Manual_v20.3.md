# QuantForge Futures Research Platform — User Manual v20.3

## Purpose
v20.3 begins the functional native migration. The browser UI remains a prototype/reference. The native architecture is now beginning to own persistent research data and deterministic research execution.

## Current functional path
1. Import an OHLCV dataset.
2. Validate chronological uniqueness and OHLC relationships.
3. Fingerprint the normalized dataset with SHA-256.
4. Persist dataset metadata and bars locally.
5. Acquire a job lease for one device/worker.
6. Load and verify the dataset fingerprint.
7. Run the deterministic EMA-cross research engine.
8. Record an evidence hash and completion event.
9. Release the lease.

## Data fidelity
v20.3 uses OHLCV bars only. It does not claim bid/ask, tick sequence, replay events, spread reconstruction, or exact intrabar order sequencing.

## Governance
Research execution remains read-only with respect to trading. The architecture prohibits live orders, live trading, arbitrary/undeclared code execution, canonical data mutation, undeclared application mutation, and manifest mutation.

## Windows and Android
The long-term application uses one shared .NET core with MAUI clients. Each device keeps local state. Logical mutable jobs use execution leases. Evidence/events are intended for future record-level synchronization.

## What is not yet production-certified
The current build host lacks the .NET SDK and native mobile/Windows SDKs. This release therefore does not claim compiled MAUI binaries or device certification.

## Visual interface
Visual refinement is intentionally paused. The v20.2 HTML/reference artifacts remain preserved for a later visual-fidelity pass after the core application is fully runnable.
