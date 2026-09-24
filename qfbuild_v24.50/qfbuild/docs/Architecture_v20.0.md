# QuantForge v20.0 Architecture Decision

## Native stack

- C# / .NET 10 LTS target.
- .NET MAUI for Windows + Android application shell.
- Shared .NET libraries for domain, governance, backtesting, bots, AI gateway, Scout, reporting, and storage.
- SQLite/local-first persistence at the device boundary.
- Immutable evidence and append-only events as synchronization records.
- Per-job execution leases prevent concurrent mutation of the same logical job.

## Device coordination

Windows and Android may operate simultaneously. They do not share a physical SQLite database file. Each node owns local state and synchronizes immutable evidence/events through a future governed sync service. A logical mutable job can have only one active execution lease.

## Python position

Python remains an optional worker technology for specialized numerical/scientific workloads. It is not the primary application architecture.

## Migration path

v19.2 HTML → native MAUI shell + preserved HTML reference → shared C# services → native radial UI fidelity → production data/storage/sync → remove transitional web dependencies where no longer needed.
