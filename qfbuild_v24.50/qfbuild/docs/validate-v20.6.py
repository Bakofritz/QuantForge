from pathlib import Path
import re, json, hashlib
root=Path(__file__).resolve().parents[1]
errors=[]
files=list((root/'src').rglob('*.cs'))
for p in files:
    s=p.read_text()
    if s.count('{')!=s.count('}'):
        errors.append(f'UNBALANCED_BRACES {p}')
for needle in ['DeterministicOptimizer','WalkForwardAnalyzer','ParameterRange','WalkForwardReport']:
    if not any(needle in p.read_text() for p in files): errors.append(f'MISSING {needle}')
proj=(root/'src/QuantForge.Runtime/QuantForge.Runtime.csproj').read_text()
if 'QuantForge.Backtesting.csproj' not in proj: errors.append('RUNTIME_MISSING_BACKTESTING_REFERENCE')
# Guardrails: optimization must not contain order/trading authority strings.
opt=(root/'src/QuantForge.Backtesting/DeterministicOptimizer.cs').read_text()
for forbidden in ['PlaceOrder','SubmitOrder','LiveTrading','BrokerOrder']:
    if forbidden in opt: errors.append(f'FORBIDDEN_AUTHORITY {forbidden}')
# Walk-forward must select on train and execute selected params on test.
w=(root/'src/QuantForge.Backtesting/WalkForwardAnalyzer.cs').read_text()
for required in ['_optimizer.Run','selected.Case.FastPeriod','selected.Case.SlowPeriod','Take(testBars)']:
    if required not in w: errors.append(f'WF_MISSING {required}')
# deterministic source hash inventory
manifest={}
for p in sorted(files):
    manifest[str(p.relative_to(root))]=hashlib.sha256(p.read_bytes()).hexdigest()
print('QuantForge v20.6 source validation')
print(f'C# files: {len(files)}')
print('Optimization contract: PASS' if not errors or all('MISSING' not in e for e in errors) else 'Optimization contract: CHECK')
print('Walk-forward contract: PASS' if all('WF_MISSING' not in e for e in errors) else 'Walk-forward contract: FAIL')
print('Architecture/guardrail checks: PASS' if not errors else 'Checks: FAIL')
if errors:
    for e in errors: print(e)
    raise SystemExit(1)
(root/'docs/Source_Hashes_v20.6.json').write_text(json.dumps(manifest,indent=2))
print('Source hash inventory written.')
