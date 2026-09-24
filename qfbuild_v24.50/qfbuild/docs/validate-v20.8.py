from pathlib import Path
import re

root=Path(__file__).resolve().parents[1]
cs=list(root.glob('src/**/*.cs'))
errors=[]
for p in cs:
    s=p.read_text(encoding='utf-8')
    if s.count('{') != s.count('}'):
        errors.append(f"Unbalanced braces: {p}")

mc=(root/'src/QuantForge.Backtesting/DeterministicMonteCarloAnalyzer.cs').read_text()
contracts=(root/'src/QuantForge.Backtesting/MonteCarloContracts.cs').read_text()
engine=(root/'src/QuantForge.Backtesting/DeterministicBacktestEngine.cs').read_text()
runtime=(root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text()
checks={
 'Monte Carlo contracts': 'MonteCarloReport' in contracts and 'MonteCarloSummary' in contracts,
 'Deterministic seed': 'DeterministicRng' in mc and 'seedFingerprint' in mc,
 'Bootstrap replacement': 'NextInt(observed.Count)' in mc,
 'Percentile statistics': 'Percentile(net, 0.05m)' in mc and 'Percentile(net, 0.95m)' in mc,
 'Observed trade path': 'TradePnlPoints' in engine,
 'Canonical gate before Monte Carlo': 'Canonical data gate blocked Monte Carlo execution' in runtime,
 'Durable checkpoint attachment': 'qf-native-v20.8' in runtime and 'SaveCheckpointAsync' in runtime,
 'Evidence linkage': 'MONTE_CARLO_COMPLETED' in runtime and 'AppendEvidenceAsync' in runtime,
 'No live trading authority': not re.search(r'PlaceOrder|SubmitOrder|LiveTrading\s*=\s*true', mc+runtime, re.I),
}
for k,v in checks.items():
    if not v: errors.append(k)
print('QuantForge v20.8 source validation')
print(f'C# files: {len(cs)}')
for k,v in checks.items(): print(f'{k}: {"PASS" if v else "FAIL"}')
if errors:
    print('ERRORS:')
    for e in errors: print(e)
    raise SystemExit(1)
