# QuantForge User Manual — v20.49

## Recovery preflight
Before a recovered research job continues, QuantForge checks its prior state, validates the checkpoint, and obtains a fresh lease. v20.49 records which checkpoint and lease were actually accepted.

## What this means
- A blocked recovery leaves a durable explanation.
- An allowed recovery is tied to a specific checkpoint and lease.
- If the expected lease disappears before the binding is recorded, recovery fails closed.
- Existing databases are upgraded with the additional preflight fields during initialization.

## Current limitation
Native runtime/device validation is still pending because the build environment does not contain the .NET SDK/runtime.
