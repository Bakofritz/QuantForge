# QuantForge v20.21 User Manual

## NT8 Triangulated Validation
QuantForge can now treat NT8 exported data as an independent reference layer for validating extracted/native data.

### Workflow
1. Inventory the NT8 source directory read-only.
2. Identify the instrument, contract, date range, and declared data type.
3. Create a small representative tick validation sample.
4. Compare extracted/native ticks against the matching NT8 Tick export.
5. Compare Minute output against NT8 Minute export.
6. Compare Day output against NT8 Day export.
7. Run coverage and integrity checks.
8. Generate deterministic validation fingerprints.
9. Only validated canonical data may enter research workflows.

### Recommended sample sizes
A few complete tick sessions are sufficient for initial parser/normalizer validation. Three to five diverse sessions are the default recommendation. A week or two is optional. Long-range Minute and Day exports can validate historical coverage without requiring a comparable multi-gigabyte tick archive.

### Validation layers
- TickDirect: event-level price/volume/timestamp agreement.
- MinuteDerived: minute OHLCV agreement.
- DayDerived: day OHLCV agreement.
- Coverage: record-count agreement.
- Integrity: chronology, uniqueness, OHLC, volume, and tick-size checks.

### Interpretation
PASS means the compared representation agrees under the configured rules for the tested range and fields. It does not prove untested periods or unavailable fields.

### Deferred contributions
Use these routing labels when the corresponding material is eventually needed:
- **NT8 REPLAY** — native replay sample.
- **NT8 TICK** — exported tick sample.
- **NT8 MINUTE** — minute history.
- **NT8 DAILY** — daily history.
- **REPLAY MATCH** — paired replay/export sample.

Do not provide large archives early unless specifically requested by a later build stage.
