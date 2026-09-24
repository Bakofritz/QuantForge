import pathlib,re,json
root=pathlib.Path(__file__).parents[1]
files=list(root.joinpath("src").rglob("*.cs"))
errors=[]
for p in files:
 s=p.read_text()
 if s.count("{")!=s.count("}"): errors.append(f"brace mismatch: {p}")
for required in ["RobustnessContracts.cs","DeterministicRobustnessAnalyzer.cs"]:
 if not (root/"src/QuantForge.Backtesting"/required).exists(): errors.append("missing "+required)
rs=(root/"src/QuantForge.Runtime/ResearchRuntime.cs").read_text()
for token in ["RunRobustnessAsync","qf-native-v20.7","ROBUSTNESS_COMPLETED","SaveCheckpointAsync"]:
 if token not in rs: errors.append("runtime missing "+token)
opt=(root/"src/QuantForge.Backtesting/DeterministicOptimizer.cs").read_text()
if "RunChunk" not in opt: errors.append("optimizer missing RunChunk")
forbidden=["PlaceOrder","SubmitOrder","LiveTrading","ModifySettings","ExecuteArbitraryCode"]
rob=(root/"src/QuantForge.Backtesting/DeterministicRobustnessAnalyzer.cs").read_text()
for x in forbidden:
 if x.lower() in rob.lower(): errors.append("forbidden authority token "+x)
print("QuantForge v20.7 source validation")
print("C# files:",len(files))
print("Robustness contract:","PASS" if not errors or not any("RobustnessContracts" in e for e in errors) else "FAIL")
print("Runtime checkpoint/evidence path:","PASS" if all(x in rs for x in ["RunRobustnessAsync","SaveCheckpointAsync","ROBUSTNESS_COMPLETED"]) else "FAIL")
print("Optimizer chunking:","PASS" if "RunChunk" in opt else "FAIL")
if errors:
 print("ERRORS:"); print("\n".join(errors)); raise SystemExit(1)
# source hash inventory
hashes={str(p.relative_to(root)):__import__('hashlib').sha256(p.read_bytes()).hexdigest() for p in files}
(root/"docs/Source_Hashes_v20.7.json").write_text(json.dumps(hashes,indent=2,sort_keys=True)+"\n")
print("Guardrail/source checks: PASS")
