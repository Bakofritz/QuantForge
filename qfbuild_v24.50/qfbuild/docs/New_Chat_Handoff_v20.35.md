# QuantForge New-Chat Handoff — v20.35

Continue the Master Build from v20.35.

Current architecture now includes isolated cases, sequential multi-case execution, worker identity, leases, heartbeats, durable checkpoints, recovery reconciliation, checkpoint-bound continuation, immutable execution receipts, receipt-derived batch accounting, and the new immutable research batch manifest integrity layer.

v20.35 adds `ResearchBatchManifest`, deterministic manifest fingerprints, `BatchManifestIntegrityService`, receipt-to-manifest validation, job-to-manifest fingerprint validation, terminal job/receipt agreement checks, duplicate terminal outcome detection, and durable integrity evidence.

Parallel execution remains disabled. Native .NET/Windows/Android runtime validation remains unavailable in the current environment.

Next planned build: v20.36 — Terminal-State Agreement + Controlled Concurrency Design Review.

Permanent build rules remain: prioritize core functionality over visual refinement; every build ships User Manual, Comprehensive Build Summary, New-Chat Handoff, Release Manifest, Validation Result, and source hashes; defer user contributions until needed.
