# QuantForge User Manual v20.47

## Worker recovery integrity
Before a worker attempts to resume a research case, QuantForge can perform a read-only consistency check across the job, terminal receipts, cancellation request, and lease.

### Safe recovery
Recovery is allowed only when no conflicting terminal result exists, cancellation has not been requested, another worker does not hold an active lease, and the job is in an eligible recovery state.

### Blocked recovery
Recovery is blocked when:
- more than one terminal receipt exists;
- a terminal receipt exists but the job is not terminal;
- the job is terminal but its receipt is missing;
- cancellation was requested;
- another worker still owns an active lease;
- the job state is not eligible for controlled recovery.

The validator does not repair these conditions automatically. This prevents the application from guessing which history is authoritative.

## Current limitations
Native execution validation requires a .NET/SQLite-capable environment. Parallel execution and live trading remain disabled.
