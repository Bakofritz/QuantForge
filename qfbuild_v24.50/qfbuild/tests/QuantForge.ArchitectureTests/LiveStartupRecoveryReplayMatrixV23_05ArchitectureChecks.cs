using QuantForge.Governance;
namespace QuantForge.ArchitectureTests;
public static class LiveStartupRecoveryReplayMatrixV23_05ArchitectureChecks
{
    public static void Validate()
    {
        var good=new LiveStartupRecoveryReplayMatrixV23_05(true,true,true,true,true,true,true,false,true);
        if(!LiveStartupRecoveryReplayGateV23_05.Evaluate(good)) throw new InvalidOperationException("Complete startup/recovery state rejected.");
        var unknown=good with {UnresolvedUnknownExecution=true};
        if(LiveStartupRecoveryReplayGateV23_05.Evaluate(unknown)) throw new InvalidOperationException("Unknown execution must block replay.");
        var audit=good with {AuditContinuityCurrent=false};
        if(LiveStartupRecoveryReplayGateV23_05.Evaluate(audit)) throw new InvalidOperationException("Stale audit continuity must block replay.");
    }
}
