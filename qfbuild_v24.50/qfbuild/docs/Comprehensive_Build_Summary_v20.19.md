# QuantForge v20.19 — Account Ledger, Intrabar Ambiguity & Risk Lock Foundation

## Purpose
v20.19 advances core execution correctness while visual refinement remains frozen. It establishes a deterministic research account ledger and an explicit OHLCV intrabar ambiguity policy so stop/target behavior is not silently inferred from incomplete bar data.

## Implemented
- Research-only account ledger with initial equity, realized/unrealized P&L, equity, peak equity, drawdown and open-contract state.
- Daily realized-loss lock using the governed $60 daily loss limit.
- Pre-entry account/risk checks remain bounded to one contract and the configured $30 maximum modeled stop risk.
- Commission and slippage are explicit ledger deductions rather than hidden in strategy P&L.
- Signed-position ledger support for both long and short research positions.
- Long and short OHLCV barrier resolvers.
- Explicit intrabar ambiguity policies: ConservativeStopFirst, TargetFirst, CloseProximity, RejectAmbiguousBar.
- Conservative default remains stop-first.
- Ambiguous-bar decisions are marked as policy-resolved rather than observed execution sequence.
- Max drawdown is derived from evolving equity.

## Governed defaults preserved
- Initial account: $3,000
- Primary instrument: MES
- Planned instrument: MNQ
- Maximum initial position: 1 contract
- Maximum modeled stop risk: $30/trade
- Daily loss lock: $60
- Initial stop: 6 MES points
- Trailing activation: +4 points
- Trailing distance: 2 points
- Profit target: disabled by default
- Commission simulation assumption: $0.95/side; verify against actual account agreement
- Slippage: 1 tick/side
- Windows Wi-Fi latency scenario: 80 ms; latency values are estimates, not measurements

## Fidelity boundary
OHLCV data does not reveal the sequence of ticks inside a bar, bid/ask, spread, routing latency, exchange matching, or exact stop/target ordering. v20.19 therefore makes ambiguous-bar handling explicit instead of claiming an observed sequence.

## Validation
- 53 C# files structurally validated with a lexical parser that ignores comments, strings and character literals.
- Account ledger contract: PASS
- Intrabar policy: PASS
- Daily loss lock: PASS
- Equity/drawdown: PASS
- Commission/slippage ledger fields: PASS
- No live-order authority: PASS
- Native compilation/device validation remains unavailable in this environment.

## Next phase
Integrate ledger accounting directly into the event-driven execution path, enforce one-exit-per-position and explicit exit precedence, attach full ledger/sensitivity reports to durable evidence, add MNQ profile without enabling it as the primary instrument, and add measured-latency telemetry interfaces. Then perform native .NET/MAUI compilation and device validation when the required SDKs are available.
