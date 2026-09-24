# QuantForge v22.05 — Detailed User Manual

## What changed
This release prepares QuantForge for native platform and broker integration while keeping live execution disabled unless every independent safety layer is satisfied.

## Native Security
The application target must provide a real OS-backed authentication/key runtime before native authentication operations can succeed. An unavailable runtime is treated as a denial, not as a fallback.

## Broker Integration
A broker connector must expose an authenticated session and normalized order results. The connector is subordinate to QuantForge's live admission, confirmation, idempotency, and recovery controls.

## Startup
At startup, QuantForge verifies audit integrity, execution-intent storage availability, and unresolved execution recovery. Any failure keeps live execution blocked.

## Recovery
If an earlier broker operation has an ambiguous outcome, reconcile that intent before any new execution is considered. Never manually retry an unknown order merely because the transport reconnects.

## Current limitation
This build does not place real broker orders and does not contain production broker credentials.
