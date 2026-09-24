# QuantForge Futures Research Platform — New-Chat Handoff v20.3

## Current state
**v20.3 Core Functionality Migration Slice**

## Architecture
C# / .NET 10 LTS + .NET MAUI + shared research libraries + local-first SQLite/evidence architecture.

## Completed in this slice
- deterministic OHLCV import with duplicate timestamp rejection;
- SHA-256 dataset fingerprinting;
- SQLite dataset/bar persistence schema;
- immutable dataset identity checks;
- fingerprint verification on dataset load;
- transactional job acquisition/renewal/release;
- deterministic EMA-cross backtest run identity;
- first end-to-end research runtime orchestration;
- evidence and append-only completion event recording;
- source/static validation and behavioral shadow tests.

## Current blocker
The build environment does not contain `dotnet`, so native C# compilation and MAUI packaging remain unverified.

## Visual directive
Visual work is frozen. Preserve v20.2 and earlier radial work as reference; do not spend core-build cycles on visual optimization until the application is substantially runnable.

## Safety
No live trading/orders. No arbitrary code execution. No canonical data mutation. External content remains untrusted. AI remains non-authoritative.

## Next action
Move to toolchain-ready compilation, native tests, crash-safe job state, then broader strategy/backtesting/governance migration.
