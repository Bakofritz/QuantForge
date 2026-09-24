# QuantForge User Manual — v20.51

## Recovery Binding Fault Validation

This release adds a validation tool for checking whether QuantForge safely refuses recovery when the approved checkpoint or worker lease no longer matches reality.

### What it checks
- checkpoint changed
- lease changed
- lease expired
- checkpoint missing
- lease missing
- wrong worker
- binding persistence failure
- identical retry
- conflicting retry

### Expected behavior
Conflicting or missing recovery state is blocked. A repeated identical binding is treated as the same logical record rather than a duplicate.

### Runtime entry point
`ValidateRecoveryPreflightBindingFaultsAsync()`

### Important limitation
The current development environment cannot execute the native .NET/SQLite runtime. Therefore this release does not claim native runtime test execution.

## Deferred User Contributions
No contribution is needed for this release. Future NT8/data requests will use the established short codes only when the relevant build phase is reached.
