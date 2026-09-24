# QuantForge User Manual — v20.22

## What changed
v20.22 connects strategy observations to a research-only account ledger. A research run can now carry a simulated position through entry, risk checks, modeled exits, commissions, slippage, and final equity accounting.

## Research-only execution chain
`Market Data → Causal Observation → Strategy Signal → Risk Gate → Simulated Fill → Position → Exit Policy → Ledger → Evidence`

This chain never sends an order to a broker.

## Account defaults
The existing governed MES defaults remain configurable and include the previously defined $3,000 research account, one-contract maximum, $60 daily loss lock, 6-point initial stop, 4-point trailing activation, 2-point trailing distance, one-tick modeled slippage per side, and the user-specified $0.95/side commission assumption. The commission figure is a simulation assumption and must not be interpreted as a current broker quote.

## Intrabar behavior
When only OHLCV is available, QuantForge cannot know whether a stop or target was touched first inside a bar. The conservative stop-first policy therefore remains explicit rather than pretending the sequence was observed.

## Latency
The existing Windows/Android latency scenarios are modeled assumptions. v20.22 adds a telemetry type that distinguishes those assumptions from future measured observations.

## Safety
- Imported strategy source remains quarantined and statically inspected.
- Canonical data remains immutable within the governed research path.
- No live orders are created.
- No broker credentials are requested.
- AI remains non-authoritative.

## User Contributions — Deferred Until Needed
Use these short codes when the relevant stage arrives:
- `NT8 REPLAY`
- `NT8 TICK`
- `NT8 MINUTE`
- `NT8 DAILY`
- `REPLAY MATCH`
- `LATENCY DATA`
