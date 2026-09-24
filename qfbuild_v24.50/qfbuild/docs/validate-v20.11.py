from pathlib import Path
import re, hashlib, json
root=Path(__file__).resolve().parents[1]
cs=list((root/'src').rglob('*.cs'))
checks=[]
def balanced(p):
    s=p.read_text()
    s=re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"|\@"(?:""|[^"])*"', '', s, flags=re.M|re.S)
    pairs={'{':'}','(':')','[':']'}
    st=[]
    for ch in s:
        if ch in pairs: st.append(pairs[ch])
        elif ch in pairs.values():
            if not st or st.pop()!=ch:return False
    return not st
checks.append(('C# balanced', all(balanced(p) for p in cs)))
scr=(root/'src/QuantForge.Governance/StrategySourceScrubber.cs').read_text()
core=(root/'src/QuantForge.Core/StrategyContracts.cs').read_text()
checks += [
 ('Strategy source artifact contract', 'StrategySourceArtifact' in core),
 ('Feature category inventory', 'StrategyFeatureCategory' in core),
 ('Safety-sensitive selection gate', 'IsSafetySensitive' in scr and 'manual review required' in scr),
 ('No source execution', 'Assembly.Load(' not in scr and 'Activator.CreateInstance(' not in scr and 'Process.Start(' not in scr),
 ('Dynamic execution detection', 'DynamicExecution' in scr),
 ('Network detection', 'NetworkAccess' in scr),
 ('Filesystem detection', 'FileSystemAccess' in scr),
 ('Credential detection', 'Authentication' in scr),
 ('Explicit component selection', 'SelectionFingerprint' in scr),
 ('Canonical model lineage', 'BuildCanonicalModel' in scr and 'SourceFingerprint' in core),
 ('Live deployment remains disabled', 'LiveDeploymentEligible' in core and 'false' in scr[scr.find('return new CanonicalStrategyModel'):]),
 ('Quarantine status', 'Quarantined' in core),
 ('Acquisition facade', (root/'src/QuantForge.Governance/StrategyAcquisitionService.cs').exists()),
 ('Governance references Core contracts', 'QuantForge.Core' in (root/'src/QuantForge.Governance/QuantForge.Governance.csproj').read_text()),
]
print('QuantForge v20.11 source validation')
print(f'C# files: {len(cs)}')
for n,v in checks: print(f'{n}: {"PASS" if v else "FAIL"}')
assert all(v for _,v in checks)
# source hashes
inv={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in cs}
(root/'docs/Source_Hashes_v20.11.json').write_text(json.dumps(inv,indent=2,sort_keys=True)+'\n')
