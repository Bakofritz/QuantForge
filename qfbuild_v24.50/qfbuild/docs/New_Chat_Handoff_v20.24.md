# QuantForge — New-Chat Handoff v20.24

## Current release
**v20.24 — Canonical Position Execution Lifecycle**

Baseline: v20.23

## Current state
QuantForge is a native-first C#/.NET 10 LTS + .NET MAUI research platform targeting Windows and Android with shared core libraries and local-first evidence architecture.

The Master Continuous Build Directive remains active.

## v20.24 completed
1. Added `CanonicalPositionLifecycleRunner`.
2. Bound canonical signed position intents to deterministic simulated fills and the account ledger.
3. Added explicit long/short/exit/reversal lifecycle semantics.
4. Protective OHLCV exits are resolved before strategy entry/reversal requests.
5. Reversals are modeled as close-then-open, never as an ambiguous position mutation.
6. Side-aware slippage is applied to simulated entry and exit fills.
7. Daily-loss-lock enforcement remains at the ledger boundary.
8. End-of-data liquidation is explicit and evidence-bearing.
9. Deterministic lifecycle engine/evidence fingerprints are generated.
10. Added architecture checks for signed short lifecycle behavior and conservative short-side ambiguity.

## Critical architecture rule
A `PositionIntent` is a research instruction, not a broker order. The lifecycle runner has no broker, live-order, or order-routing authority.

## Current execution chain
`MARKET EVENT`
→ causal timeframe observation
→ strategy / canonical position intent
→ protective exit resolution
→ risk gate
→ side-aware simulated fill
→ position state
→ account ledger
→ evidence fingerprint

## Reversal rule
A reversal is always represented as two lifecycle actions:
1. close current position;
2. if the ledger still permits entry, open the opposite position.

If the closing trade triggers the daily loss lock, the new position is rejected.

## Fidelity rule
OHLCV does not provide observed intrabar event order. The lifecycle uses an explicit ambiguity policy and records the fidelity limitation. Tick/replay/bid/ask fidelity requires corresponding event sources.

## Validation limitation
The current environment does not contain the .NET SDK/compiler, Android SDK/device, or Windows native runtime. Source and structural validation therefore remain distinct from native runtime validation.

## Visual status
Visual refinement remains intentionally frozen. Continue core functionality, data integrity, governance, persistence, research execution, and validation before returning to visual refinement.

## Deferred user contributions
Do not request them early.
- NT8 REPLAY
- NT8 TICK
- NT8 MINUTE
- NT8 DAILY
- REPLAY MATCH
- LATENCY DATA

## Next build
**v20.25 — Account-Ledger Session Integrity + Trade Evidence Hardening**

Continue autonomously from this handoff without asking the user to restate the architecture.
