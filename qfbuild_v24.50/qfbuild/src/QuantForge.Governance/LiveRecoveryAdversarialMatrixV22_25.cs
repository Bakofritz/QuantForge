using System.Collections.Generic;

namespace QuantForge.Governance;

public sealed record RecoveryAdversarialCaseV22_25(string Name, bool MustBlock, string RequiredOutcome);

public static class RecoveryAdversarialMatrixV22_25
{
    public static IReadOnlyList<RecoveryAdversarialCaseV22_25> Cases => new RecoveryAdversarialCaseV22_25[]
    {
        new("Submit timeout before broker acknowledgement", true, "Remain Unknown; reconcile externally; never blind-retry"),
        new("Disconnect after submission", true, "Persist execution identity and require broker reconciliation"),
        new("Restart with unresolved unknown execution", true, "RecoveryRequired"),
        new("Broker returns unmapped order state", true, "Non-definitive state; block recovery"),
        new("Authority changes during recovery", true, "Reject stale authorization binding"),
        new("Audit-chain verification failure", true, "Blocked"),
        new("All identities and gates reconcile cleanly", false, "RecoveryCleared")
    };
}
