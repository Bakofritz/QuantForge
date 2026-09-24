# QuantForge v22.75 User Manual

1. Use Research/Backtesting without enabling live trading.
2. Import strategies through quarantine, static audit, feature selection, and governance review.
3. Treat the Live Environment State panel as informational and fail-closed: capability availability does not itself grant order authority.
4. Before any live continuation, verify secure runtime, startup integrity, current audit evidence, authenticated broker session, broker heartbeat, healthy risk state, cleared recovery, current authority, explicit armed state, and complete recovery evidence.
5. If any condition becomes stale, mismatched, unknown, or unavailable, stop and require revalidation/reconciliation.
6. Never interpret a reconnect, session renewal, or broker observation as permission to submit an order.

No production broker order submission is claimed by this release.
