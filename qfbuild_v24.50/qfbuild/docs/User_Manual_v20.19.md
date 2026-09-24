# QuantForge v20.19 User Manual

## What changed
v20.19 adds the research account ledger and explicit OHLCV intrabar ambiguity handling.

### Account ledger
The ledger tracks:
- starting equity
- realized P&L
- unrealized P&L
- current equity
- peak equity
- maximum drawdown
- daily realized P&L
- daily loss-lock state
- open contract count

### Risk gate
The governed MES profile remains one contract with a $30 modeled stop-risk ceiling and a $60 daily loss lock. Commission and slippage are tracked separately as execution friction.

### Intrabar ambiguity
When a completed OHLCV bar reaches both a stop and target, QuantForge does not pretend to know which occurred first. The resolver records the selected policy. The default is `ConservativeStopFirst`.

Available policies:
1. ConservativeStopFirst — stop is assumed first when both barriers are touched.
2. TargetFirst — target is assumed first.
3. CloseProximity — the barrier closer to the closing price is selected.
4. RejectAmbiguousBar — the bar is marked ambiguous and not treated as a confirmed execution sequence.

These are research assumptions, not observed market microstructure.

## Safety
Imported scripts remain quarantined and are never executed by the source scrubber. QuantForge has no live broker/order authority in this build.

## Current fidelity
The engine remains OHLCV/bar-driven. Exact tick ordering, bid/ask, spread, broker routing, exchange matching and measured network latency require additional data/telemetry.
