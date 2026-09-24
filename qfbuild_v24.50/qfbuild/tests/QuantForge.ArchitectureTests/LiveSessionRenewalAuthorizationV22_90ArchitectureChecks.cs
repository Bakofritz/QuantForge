using QuantForge.Governance;
namespace QuantForge.ArchitectureTests;
public static class LiveSessionRenewalAuthorizationV22_90ArchitectureChecks
{
    public static void Validate()
    {
        var now=DateTime.UtcNow;
        var r=new LiveSessionRenewalAuthorizationV22_90("s1","a1","l1",now,now.AddMinutes(5));
        if(!LiveSessionRenewalAuthorizationGateV22_90.CanRenew(r,"s1","a1","l1",now)) throw new InvalidOperationException("Valid renewal rejected.");
        if(LiveSessionRenewalAuthorizationGateV22_90.CanRenew(r,"s1","a2","l1",now)) throw new InvalidOperationException("Authority mismatch must block renewal.");
        if(LiveSessionRenewalAuthorizationGateV22_90.CanRenew(r,"s1","a1","l2",now)) throw new InvalidOperationException("Prior lease mismatch must block renewal.");
    }
}
