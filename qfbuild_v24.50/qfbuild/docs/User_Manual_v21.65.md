# QuantForge v21.65 — Detailed User Manual

## Research Mode
Research and backtesting remain the normal operating mode. Imported strategies remain governed by the existing quarantine, audit, feature-selection, capability, and research-registration pipeline.

## Live Security
Live trading remains disabled by default. Enabling the pathway still requires the previously established fresh login reauthentication and two independent step-up factors.

## Native Authentication
The application now exposes a platform bridge for device-bound authentication and an independent second factor. The bridge reports unavailable rather than pretending authentication succeeded when the platform adapter has not been configured.

## Secure Key Storage
A secure-key-store interface is available for platform-native Android/Windows implementations. The included fallback deliberately throws `PlatformNotSupportedException` when native secure storage is unavailable.

## Execution Persistence
Before broker submission, an execution intent is persisted with a unique idempotency key. A restart can therefore recover the intent rather than creating a new submission.

## Broker Request
The request serializer produces deterministic canonical fields and a request fingerprint. A future broker connector must use this canonical representation rather than reconstructing an order independently.

## Ambiguous Results
If submission produces an unknown outcome, QuantForge blocks retry. The user/operator must reconcile the existing idempotency key with the broker.

## Recovery
At startup or before a new live execution batch, the recovery gate checks known intents. Any `Unknown` intent blocks additional execution until reconciled.

## Important Limitation
Native Android/Windows authentication and secure-key implementations, real broker transport, and production live order submission are not included or claimed as validated in this build.
