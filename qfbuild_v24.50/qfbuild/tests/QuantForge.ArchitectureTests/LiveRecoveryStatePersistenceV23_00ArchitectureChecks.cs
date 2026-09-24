using QuantForge.Governance;
namespace QuantForge.ArchitectureTests;
public static class LiveRecoveryStatePersistenceV23_00ArchitectureChecks
{
    public static void Validate()
    {
        var now=DateTime.UtcNow;
        var s=new LiveRecoveryStatePersistenceV23_00("e1","i1",LiveRecoveryStageV22_55.Unknown,"fp1",null,now);
        if(!LiveRecoveryStateRestorationGateV23_00.CanRestore(s,now,TimeSpan.FromMinutes(10))) throw new InvalidOperationException("Valid persisted recovery state rejected.");
        if(!LiveRecoveryStateRestorationGateV23_00.RequiresReconciliation(s)) throw new InvalidOperationException("Unknown state must require reconciliation.");
        if(LiveRecoveryStateRestorationGateV23_00.CanRestore(s with {PersistedAtUtc=now.AddMinutes(-20)},now,TimeSpan.FromMinutes(10))) throw new InvalidOperationException("Stale recovery state must fail closed.");
    }
}
