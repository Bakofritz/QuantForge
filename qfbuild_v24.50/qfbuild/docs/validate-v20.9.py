from pathlib import Path
import re, hashlib, json
root=Path(__file__).resolve().parents[1]
cs=list((root/'src').rglob('*.cs'))
fail=[]
for p in cs:
    s=p.read_text(encoding='utf-8')
    if s.count('{')!=s.count('}'):
        fail.append(f'Unbalanced braces: {p}')
required=[
 root/'src/QuantForge.Core/ResearchProgress.cs',
 root/'src/QuantForge.Storage/JobStoreContracts.cs',
 root/'src/QuantForge.Storage/SqliteLocalStore.cs',
 root/'src/QuantForge.Backtesting/DeterministicMonteCarloAnalyzer.cs',
 root/'src/QuantForge.Runtime/ResearchRuntime.cs']
for p in required:
    if not p.exists(): fail.append(f'Missing: {p}')
runtime=(root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text()
store=(root/'src/QuantForge.Storage/SqliteLocalStore.cs').read_text()
ana=(root/'src/QuantForge.Backtesting/DeterministicMonteCarloAnalyzer.cs').read_text()
checks={
 'progress persistence': 'SaveProgressAsync' in store and 'LoadProgressAsync' in store,
 'progress table': 'research_progress' in store,
 'fingerprint-bound resume': 'Stored Monte Carlo progress cannot be resumed' in runtime,
 'integrity hash verification': 'failed integrity verification' in runtime,
 'cancellation check': 'LoadCancellationAsync' in runtime,
 'lease renewal': 'RenewAsync' in runtime,
 'chunk execution': 'RunChunk' in runtime and 'RunChunk' in ana,
 'durable checkpoint': 'SaveCheckpointAsync' in runtime,
 'final evidence': 'MONTE_CARLO_COMPLETED' in runtime,
 'no live order authority': not re.search(r'PlaceLiveOrder|SubmitLiveOrder|EnableLiveTrading', runtime+ana),
}
for k,v in checks.items():
    if not v: fail.append(f'FAIL: {k}')
out=['QuantForge v20.9 source validation',f'C# files: {len(cs)}']
out += [f'{k}: '+('PASS' if v else 'FAIL') for k,v in checks.items()]
if fail: out += fail
(root/'docs/Validation_Result_v20.9.txt').write_text('\n'.join(out)+'\n',encoding='utf-8')
# source hashes
hashes={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((root/'src').rglob('*')) if p.is_file()}
(root/'docs/Source_Hashes_v20.9.json').write_text(json.dumps(hashes,indent=2,sort_keys=True)+'\n',encoding='utf-8')
print('\n'.join(out))
raise SystemExit(1 if fail else 0)
