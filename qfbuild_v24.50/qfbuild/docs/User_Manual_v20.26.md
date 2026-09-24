# QuantForge User Manual v20.26

## 1. Purpose
v20.26 introduces the research-context isolation layer used when QuantForge evaluates multiple strategies, instruments, timeframes, or parameter cases.

## 2. Isolation model
Each research case receives a deterministic context identity and independent mutable state. Dataset identity is carried by immutable fingerprint rather than copied mutable market state.

## 3. What is isolated
- strategy state
- position state
- per-case evidence references
- case configuration identity

The durable account ledger remains a research component of the individual execution lifecycle; future case execution will attach its own ledger instance to the context.

## 4. What may be shared
Validated immutable datasets may be referenced by dataset ID and fingerprint. Sharing a dataset does not share strategy or position state.

## 5. Multiple research cases
A future multi-case run can represent MES Strategy A, MES Strategy B, MNQ Strategy A, or different timeframes as separate contexts. Duplicate context identities are rejected.

## 6. Safety
Contexts have research authority only. This feature does not create broker authority, live orders, arbitrary code execution, or AI application mutation authority.

## 7. Fidelity
Isolation does not change the fidelity of the source data. OHLCV remains OHLCV unless actual higher-fidelity event data is supplied and validated.

## 8. Current validation limitation
The current development environment does not provide the .NET SDK/compiler, Android SDK/device, or Windows native runtime. Source-level validation is therefore reported separately from native runtime validation.

## 9. Deferred contributions
Use the established codes only when requested at the appropriate stage: NT8 REPLAY, NT8 TICK, NT8 MINUTE, NT8 DAILY, REPLAY MATCH, LATENCY DATA.
