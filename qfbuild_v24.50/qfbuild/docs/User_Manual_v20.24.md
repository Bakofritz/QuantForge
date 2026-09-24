# QuantForge Futures Research Platform — User Manual v20.24

## What changed
v20.24 adds the canonical position execution lifecycle. QuantForge can now represent a research strategy's intended position side and carry that intent through simulated fills, position state, account-ledger accounting, and evidence.

## Position intents
The supported research intents are:
- Hold
- Enter Long
- Enter Short
- Exit
- Reverse to Long
- Reverse to Short

These are research instructions only. They are not broker orders.

## Long and short simulation
The execution model applies adverse modeled slippage by side:
- Long entry moves the simulated fill upward.
- Short entry moves the simulated fill downward.
- Long exit moves the simulated fill downward.
- Short exit moves the simulated fill upward.

Commission and slippage remain explicit research costs.

## Protective exits
When a position is open, QuantForge evaluates declared stop, target, and trailing rules using the selected OHLCV ambiguity policy.

The default lifecycle policy is ConservativeStopFirst.

If a protective exit occurs, it is processed before a new strategy entry or reversal request on that event.

## Reversals
A reversal is not treated as an instantaneous broker operation. QuantForge models:
1. close the existing position;
2. apply commission/slippage and ledger accounting;
3. check the risk ledger;
4. open the opposite position only if permitted.

This makes the research record auditable.

## Daily loss lock
The account ledger enforces the configured daily loss limit. A losing reversal exit can therefore prevent the new opposite-side entry when the daily loss limit has been reached.

## End of data
An open research position is explicitly closed at the final available bar and labeled `END_OF_DATA`. This is not represented as a strategy-generated signal.

## Data fidelity
OHLCV bars cannot prove the order in which intrabar prices were reached. QuantForge therefore records the declared ambiguity policy rather than claiming observed tick execution.

Higher-fidelity tick/replay/bid/ask behavior requires corresponding data.

## Safety
v20.24 does not provide:
- live orders;
- broker order routing;
- automatic live deployment;
- arbitrary imported-source execution;
- unrestricted AI application mutation.

## Deferred contributions
Do not provide data until the relevant build stage requests it.

Codes:
- NT8 REPLAY
- NT8 TICK
- NT8 MINUTE
- NT8 DAILY
- REPLAY MATCH
- LATENCY DATA

## Build status
Native compilation and device validation are pending a suitable .NET/MAUI build environment. Source-level validation is reported separately.
