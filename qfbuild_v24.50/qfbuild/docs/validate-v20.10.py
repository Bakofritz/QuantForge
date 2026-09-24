from pathlib import Path
root=Path(__file__).resolve().parents[1]
cs=list(root.glob('src/**/*.cs'))
checks={
 'all C# files balanced': all(x.read_text().count('{')==x.read_text().count('}') and x.read_text().count('(')==x.read_text().count(')') for x in cs),
 'optimization resumable runtime': 'RunOptimizationAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text() and 'OPTIMIZATION' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text(),
 'robustness resumable runtime': 'RunRobustnessResumableAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text(),
 'walk-forward resumable runtime': 'RunWalkForwardResumableAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text(),
 'persistent aggregation': 'SaveProgressAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text(),
 'lease renewal per chunk': 'EnsureCanContinueAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text() and 'RenewAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text(),
 'cancellation per chunk': 'LoadCancellationAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text(),
 'duplicate evidence guard': 'ContainsEvidenceAsync' in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text(),
 'no live order authority': not any(t in (root/'src/QuantForge.Runtime/ResearchRuntime.cs').read_text() for t in ['SubmitOrder','PlaceOrder','BrokerOrder','SendOrder']),
}
print('QuantForge v20.10 source validation')
print(f'C# files: {len(cs)}')
for k,v in checks.items(): print(f'{k}: {"PASS" if v else "FAIL"}')
if not all(checks.values()): raise SystemExit(1)
