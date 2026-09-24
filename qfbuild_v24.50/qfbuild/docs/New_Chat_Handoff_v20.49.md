# QuantForge v20.49 — New Chat Handoff

Continue from v20.49. The recovery preflight receipt is now bound to the exact checkpoint sequence/cursor/state hash and accepted lease version/fingerprint used by controlled recovery. Blocked preflight decisions remain recorded. Ready recovery records its binding only after fresh lease acquisition. SQLite schema migration preserves existing databases by adding missing columns during initialization. Native .NET/SQLite execution remains unavailable in the current environment. Next: v20.50 recovery binding reconciliation/fault tests.
