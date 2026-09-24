# QuantForge Futures Research Platform — Comprehensive Build Summary / Engineering Log v20.0

## 1. Build identity
- Product: QuantForge Futures Research Platform
- Release: v20.0 Migration Foundation
- Baseline: v19.2
- Native target: C# / .NET 10 LTS + .NET MAUI
- Transitional HTML artifact: `QuantForge_Futures_Backtester_v20.0.html`
- HTML size: 206284 bytes
- HTML SHA-256: `c95bdbf08b2394bd4b0e69bd9b83e5ab1f49fc1a7f4430ab31b6a0cfb06af4c1`
- Embedded JavaScript blocks: 28
- JavaScript syntax validation: 28/28 passed `node --check`

## 2. Authorization state
The Master Continuous Build Directive is active. This release follows build → test → document → integrate → validate → continue, stopping only where the environment creates a genuine technical boundary.

## 3. Major architectural decision implemented
The single-file HTML application is no longer treated as the eventual production architecture. The native target is a shared .NET core with .NET MAUI clients for Windows and Android.

Python is intentionally not the primary application language. It remains available as an optional specialized numerical/scientific worker technology behind governed interfaces.

## 4. Native project foundation
Created solution structure:
- QuantForge.App
- QuantForge.Core
- QuantForge.Data
- QuantForge.Governance
- QuantForge.Backtesting
- QuantForge.Bots
- QuantForge.AI
- QuantForge.Scout
- QuantForge.Reporting
- QuantForge.Storage
- QuantForge.ArchitectureTests

The MAUI application targets Windows and Android and references the shared libraries.

## 5. Governance contracts
The foundation explicitly models:
- authority layers;
- prohibited authorities;
- research job states;
- execution leases;
- immutable evidence records;
- append-only research events;
- structured AI directives and non-authoritative AI analysis;
- Scout candidate quarantine and safety audit;
- bot manifests and governed commands.

## 6. Windows + Android coordination
The intended production model permits both devices to run simultaneously. Each device owns local state. A logical mutable research job is protected by an execution lease so the same job cannot be mutated by two nodes at once. Immutable evidence and event records are the synchronization primitive for future cross-device coordination.

The design deliberately avoids a shared physical SQLite database file across a network.

## 7. Visual fidelity implementation
The v19.2 radial UI is preserved as a reference asset. The v20 transitional HTML receives a visual-fidelity pass that strengthens:
- three-column proportions;
- central radial workspace size;
- 430px desktop Research Focus;
- eight-petal placement and dimensions;
- orbital ring framing;
- contextual rails;
- responsive breakpoints.

The authoritative visual requirement is documented in `Visual_Reference_Contract_v20.0.md`.

## 8. Documentation
Included:
- README
- Architecture decision
- Visual reference contract
- v20.0 User Manual
- Comprehensive Build Summary
- New-Chat Handoff
- validation script
- preserved v19.2 visual reference

## 9. Validation
### Passed
- HTML file exists.
- Native project structure exists.
- Required project references exist.
- 28/28 embedded JavaScript blocks pass `node --check`.
- Required governance contracts exist.
- Visual reference artifact preserved.

### Not available in this environment
- .NET SDK compilation: `dotnet` is not installed.
- Android build/package validation: not available without .NET/Android SDK.
- Windows package validation: not available without .NET/Windows SDK.
- Native SQLite runtime validation: implementation not yet installed.
- Native device integration validation: not available.
- Automated Chromium screenshot capture: attempted at 1536x1024 and timed out before producing an image; therefore no pixel-perfect browser pass is claimed.

## 10. Safety boundary
No live trading, live order, arbitrary code execution, canonical data mutation, manifest mutation, or undeclared application mutation was added.

## 11. Data fidelity
v20.0 does not upgrade market-data fidelity. Existing OHLCV claims remain OHLCV unless a corresponding bid/ask, tick, replay, or event source is actually supplied.

## 12. Next build sequence
1. Install/validate .NET 10 + MAUI toolchain in a suitable build environment.
2. Compile the solution and resolve warnings/errors.
3. Implement native SQLite repository and migration schema.
4. Implement lease/heartbeat/recovery state machine.
5. Port governed backtest engine into shared C# services.
6. Port Scout/quarantine and script-scrubber services.
7. Replace native-shell placeholders with functional radial navigation while preserving the visual contract.
8. Establish automated desktop/mobile screenshot comparison and structural UI assertions.
