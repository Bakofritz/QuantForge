# QuantForge v24.50 — Research Product Candidate Gate

## Release
- v24.50: Research Product Candidate Gate
- Cumulative base: v24.45
- Live brokerage and live order submission remain hard-disabled.

## New capability
- Added Research Product Candidate Gate as a bounded research/simulation control.
- No new broker, order-routing, or application-setting authority is granted.

## Stability policy
- Fail-closed validation remains mandatory.
- Market-data access remains read-only.
- Research execution remains simulation-only.

## Live-environment state
- Live brokerage connection: DISABLED
- Live order submission: DISABLED
- Simulation: ENABLED
- Market data: READ-ONLY
- NT8: future prerequisite only; running NT8 does not unlock live trading
- AI research: read-only/simulation-only

## Source inventory
- C# source files under src: 217
- Total C# files including architecture checks: 225
- Native .NET compilation is not claimed unless separately validated.
