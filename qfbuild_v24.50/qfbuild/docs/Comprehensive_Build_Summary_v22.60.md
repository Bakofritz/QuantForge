# QuantForge Master Build v22.60

## Release purpose
Introduces authority-bound broker session leases with explicit expiry and renewal eligibility; expired or authority-mismatched leases fail closed.

## Simple live-environment state and capabilities
- Research/backtesting: independently available and does not grant live order authority.
- Read-only market-data/account observation: separately governed from order routing.
- Broker session: authenticated, identity-bound, and freshness-checked where required.
- Audit state: current, continuity-verified evidence is required for live continuation.
- Risk state: must remain healthy.
- Recovery: unresolved/unknown execution outcomes remain blocked until reconciled.
- Authority: must match the current provenance/fingerprint and be revalidated after material changes.
- Live pathway: explicitly armed; no reconnect or renewal automatically arms it.
- Live order routing: remains behind the complete fail-closed gate.
- Production live-money order submission: not implemented or claimed by this architecture-validation build.

## Engineering additions
Introduces authority-bound broker session leases with explicit expiry and renewal eligibility; expired or authority-mismatched leases fail closed.

Previous build: 22.55
This release preserves the separation of research authority, live pathway authority, bot authority, order authority, and broker execution authority.
