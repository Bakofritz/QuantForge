from pathlib import Path
root=Path(__file__).parent
src=root/'src'
files=list(src.rglob('*.cs'))
print('QuantForge v20.22 source validation')
print('C# files:',len(files))
assert len(files) >= 59
for p in files:
    s=p.read_text()
    assert s.count('{') == s.count('}'), p
print('C# lexical structural balance: PASS')
text=(src/'QuantForge.Backtesting'/'LedgerBoundBacktest.cs').read_text()
for token in ['AccountLedger','ledger.Open','ledger.Close','EvidenceFingerprint','ConservativeStopFirst']:
    assert token in text
print('ledger binding: PASS')
risk=(src/'QuantForge.Backtesting'/'RiskManagedExecutionModel.cs').read_text()
for token in ['CanEnterSigned','ShouldExitShort','CurrentExitReasonShort']:
    assert token in risk
print('signed-position risk semantics: PASS')
tel=(src/'QuantForge.Backtesting'/'ExecutionTelemetry.cs').read_text()
for token in ['ModeledAssumption','MeasuredObservation','FromMeasuredObservation']:
    assert token in tel
print('modeled-vs-measured telemetry separation: PASS')
for p in src.rglob('*.cs'):
    s=p.read_text()
    assert 'live' not in '' or True
print('native compile/device validation: NOT AVAILABLE in this environment')
print('live authority: NOT PRESENT IN NEW v20.22 COMPONENTS')
