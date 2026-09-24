from pathlib import Path
import hashlib, shutil, subprocess
ROOT=Path(__file__).resolve().parents[1]

def strip_cs(s):
    out=[]; i=0; state='code'
    while i<len(s):
        c=s[i]
        if state=='code':
            if c=='/' and i+1<len(s) and s[i+1]=='/': state='line'; out+=[' ',' ']; i+=2; continue
            if c=='/' and i+1<len(s) and s[i+1]=='*': state='block'; out+=[' ',' ']; i+=2; continue
            if c=='"': state='string'; out.append(' '); i+=1; continue
            if c=="'": state='char'; out.append(' '); i+=1; continue
            out.append(c); i+=1
        elif state=='line':
            out.append('\n' if c=='\n' else ' '); i+=1
            if c=='\n': state='code'
        elif state=='block':
            if c=='*' and i+1<len(s) and s[i+1]=='/': out+=[' ',' ']; i+=2; state='code'
            else: out.append('\n' if c=='\n' else ' '); i+=1
        elif state=='string':
            if c=='\\': out += [' ',' ']; i+=2; continue
            if c=='"': state='code'
            out.append(' '); i+=1
        else:
            if c=='\\': out += [' ',' ']; i+=2; continue
            if c=="'": state='code'
            out.append(' '); i+=1
    return ''.join(out)

files=list(ROOT.rglob('*.cs'))
issues=[]
for p in files:
    s=strip_cs(p.read_text(errors='replace'))
    for a,b in [('{','}'),('(',')'),('[',']')]:
        if s.count(a)!=s.count(b): issues.append(str(p.relative_to(ROOT))); break
checks={
 'C# structural balance': not issues,
 'MultiCaseExecutionCoordinator exists': (ROOT/'src/QuantForge.Runtime/MultiCaseExecutionCoordinator.cs').exists(),
 'per-case lease call': 'TryAcquireAsync(id.JobId' in (ROOT/'src/QuantForge.Runtime/MultiCaseExecutionCoordinator.cs').read_text(),
 'per-case release': 'ReleaseAsync(id.JobId' in (ROOT/'src/QuantForge.Runtime/MultiCaseExecutionCoordinator.cs').read_text(),
 'max case guard': 'request.Cases.Count > request.MaxCases' in (ROOT/'src/QuantForge.Runtime/MultiCaseExecutionCoordinator.cs').read_text(),
 'aggregate fingerprint': 'ResearchFingerprint.Sha256' in (ROOT/'src/QuantForge.Runtime/MultiCaseExecutionCoordinator.cs').read_text(),
 'isolation coordinator used': 'IsolatedResearchCoordinator' in (ROOT/'src/QuantForge.Runtime/MultiCaseExecutionCoordinator.cs').read_text(),
 'research-only prohibited authority set': 'LIVE_ORDER' in (ROOT/'src/QuantForge.Core/Contracts.cs').read_text(),
}
for k,v in checks.items(): print(('PASS: ' if v else 'FAIL: ')+k)
print(f'INFO: C# source files = {len(files)}')
print('NOT RUN: native dotnet build — .NET SDK/compiler unavailable' if not shutil.which('dotnet') else 'INFO: dotnet available')
print('NOT RUN: Android runtime/device validation — Android SDK/device unavailable')
print('NOT RUN: Windows native runtime validation — Windows SDK/runtime unavailable')
raise SystemExit(1 if issues or not all(checks.values()) else 0)
