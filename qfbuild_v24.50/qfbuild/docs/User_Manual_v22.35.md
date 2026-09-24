# QuantForge v22.35 User Manual

## Live environment status
The live-environment panel should be interpreted as a current-state snapshot, not a permanent permission grant.

### Simple state/capability summary
- Research/backtesting: independent of live order authority.
- Live market-data observation: may remain available when broker authentication is healthy.
- Account/position observation: capability-dependent and separate from order authority.
- Broker session: must remain authenticated and identity-bound.
- Audit verification: must remain healthy for live recovery/continuation.
- Risk state: must remain healthy.
- Recovery: must remain cleared.
- Live pathway: must remain explicitly armed.
- Order routing: unavailable if any required condition changes.
- Revalidation: required before continuing after a material environment or authority change.
- Unknown execution outcomes: remain blocked until reconciled.
- Reconnect: never automatically re-arms the live pathway.
- Production live-money execution: not implemented or claimed by v22.35.

## Architecture intent
Capability provenance binds the current environment to the identities and health conditions under which admission was granted. The revalidation gate prevents stale authority from being reused after a session, authority, audit, risk, recovery, or armed-state change.
