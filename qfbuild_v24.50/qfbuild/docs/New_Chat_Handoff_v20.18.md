# QuantForge New-Chat Handoff v20.18

Current baseline: v20.18.

v20.18 adds explicit fixed-point profit-target semantics, pre-entry risk-budget enforcement, event-driven risk-managed entry binding, and durable evidence-linked execution sensitivity reporting.

Preserve defaults:
- $3,000 account
- MES primary
- MNQ planned
- 1-contract initial limit
- 6-point initial stop
- 4-point trailing activation / 2-point trail
- profit target disabled by default unless explicitly configured
- $30 stop-risk budget
- $60 daily loss lock
- $0.95/side user simulation commission assumption
- 1 tick/side slippage
- Windows Wi-Fi default scenario with 80 ms estimated round trip

Important semantic boundary: the $30 risk budget currently applies to modeled stop-loss price exposure; commission/slippage are separately reported execution friction. Do not silently change this definition.

Continue next with conservative OHLCV intrabar ambiguity handling, short-side risk semantics, MNQ-specific execution profile, durable sensitivity-report attachment, and full account-equity/ledger accounting.

Visual refinement remains deferred. Native .NET/MAUI compile and Windows/Android device validation remain required before runtime claims. Every build must include User Manual, handoff, comprehensive summary, release metadata, validation, limitations, fidelity boundaries, and hashes.
