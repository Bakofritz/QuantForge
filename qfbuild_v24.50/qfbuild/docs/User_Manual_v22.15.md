# QuantForge v22.15 — Detailed User Manual

## Purpose
This release strengthens the boundary between platform security, broker state, and live execution authority.

### Native security
The application requests a platform secure-key runtime. If it is unavailable, protected signing operations fail closed. There is no plaintext-key fallback.

### Broker state
Broker-specific states are normalized into a controlled QuantForge state set. Unknown broker states remain non-definitive.

### Recovery
After restart, disconnect, timeout, or ambiguous acknowledgement, QuantForge must reconcile the original execution identity before another live execution can be considered.

### User safety
A broker reconnect does not re-arm live trading. The final live-order admission gate remains authoritative and must be revalidated after recovery.
