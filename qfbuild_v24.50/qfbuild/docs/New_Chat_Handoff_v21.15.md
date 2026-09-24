# QuantForge v21.15 New-Chat Handoff

Continue from v21.15. The strategy governance lifecycle from v21.10 is preserved. v21.15 adds the secured live-trading pathway framework.

## Current architecture
Research import -> quarantine -> audit -> component selection -> governance -> canonical strategy -> research registry -> governed research execution/backtesting/optimization.

Live pathway is separate: security settings -> fresh reauthentication -> two independent step-up factors -> communication dependency validation -> strategy/risk/runtime bindings -> explicit arm -> expiring live session -> bot/order capability gate.

## Next recommended consolidation
Integrate the live pathway with persistent settings storage, platform-native authentication adapters, broker connector capability discovery, risk-health heartbeats, and an explicit live-order adapter that remains disabled until every gate passes. Then build the user-facing Security & Live Trading settings page and dependency-aware communication controls.
