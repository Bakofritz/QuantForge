using QuantForge.Governance;
namespace QuantForge.ArchitectureTests;
public static class LiveAuditContinuityCheckpointV22_95ArchitectureChecks
{
    public static void Validate()
    {
        var now=DateTime.UtcNow;
        var c=new LiveAuditContinuityCheckpointV22_95("h1","h1","cp1",now);
        if(!LiveAuditContinuityCheckpointGateV22_95.CanResume(c,now,TimeSpan.FromMinutes(5))) throw new InvalidOperationException("Current checkpoint rejected.");
        if(LiveAuditContinuityCheckpointGateV22_95.CanResume(c with {VerifiedHead="h2"},now,TimeSpan.FromMinutes(5))) throw new InvalidOperationException("Audit fork must block resume.");
        if(LiveAuditContinuityCheckpointGateV22_95.CanResume(c with {PersistedAtUtc=now.AddMinutes(-10)},now,TimeSpan.FromMinutes(5))) throw new InvalidOperationException("Stale checkpoint must block resume.");
    }
}
