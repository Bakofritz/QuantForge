from pathlib import Path
import re, sys
root=Path(__file__).resolve().parents[1]
ledger=(root/'src/QuantForge.Backtesting/AccountLedger.cs').read_text()
storage=(root/'src/QuantForge.Storage/SqliteLocalStore.cs').read_text()
runtime=(root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text()
tests=(root/'tests/QuantForge.ArchitectureTests/PositionLifecycleChecks.cs').read_text()
checks={
 'ledger accounting event contract':'LedgerAccountingEvent' in ledger,
 'UTC session declaration':'MODE=UTC_CALENDAR_DAY' in ledger,
 'session start event':'SESSION_STARTED' in ledger,
 'session roll event':'SESSION_ROLLED' in ledger,
 'trade sequence identity':'_tradeSequence' in ledger and 'LEDGER_TRADE_V20.25' in ledger and 'tradeSequence:D8' in ledger,
 'daily lock transition':'DAILY_LOSS_LOCKED' in ledger,
 'durable trade evidence table':'ledger_trade_evidence' in storage,
 'durable ledger event table':'ledger_accounting_events' in storage,
 'immutable trade conflict':'Immutable trade evidence conflict detected' in storage,
 'immutable ledger event conflict':'Immutable ledger event conflict detected' in storage,
 'immutable evidence conflict':'Immutable evidence conflict detected' in storage,
 'runtime lifecycle evidence binder':'RecordPositionLifecycleEvidenceAsync' in runtime,
 'same timestamp test':'Same-timestamp trades must remain deterministically unique' in tests,
 'daily lock test':'DAILY_LOSS_LOCKED' in tests,
 'session rollover test':'SESSION_ROLLED' in tests,
}
failed=[k for k,v in checks.items() if not v]
for k,v in checks.items(): print(('PASS ' if v else 'FAIL ')+k)
if failed: sys.exit(1)
print('PASS all v20.25 source/architecture checks')
