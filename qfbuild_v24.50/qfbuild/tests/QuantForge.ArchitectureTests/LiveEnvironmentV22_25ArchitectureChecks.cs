using System;
using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class LiveEnvironmentV22_25ArchitectureChecks
{
    public static void Run()
    {
        var blocked = LiveEnvironmentStateBuilderV22_25.Build(false, true, true, true, true, true, true);
        if (blocked.State != LiveEnvironmentStateV22_25.Blocked || blocked.CanRouteOrders) throw new InvalidOperationException("Secure runtime absence must block live routing.");
        var recovery = LiveEnvironmentStateBuilderV22_25.Build(true, true, true, true, true, false, false);
        if (recovery.State != LiveEnvironmentStateV22_25.RecoveryRequired) throw new InvalidOperationException("Uncleared recovery must remain gated.");
        var ready = LiveEnvironmentStateBuilderV22_25.Build(true, true, true, true, true, true, true);
        if (!ready.CanRouteOrders) throw new InvalidOperationException("Complete gate set must permit the routing capability state.");
        var a = new RecoveryAuthorizationBindingV22_25("e1", "k1", "r1", "a1");
        var b = new RecoveryAuthorizationBindingV22_25("e1", "k1", "r1", "a2");
        if (RecoveryAuthorizationBindingV22_25Policy.Matches(a, b)) throw new InvalidOperationException("Changed authority binding must not match.");
        foreach (var c in RecoveryAdversarialMatrixV22_25.Cases) if (c.MustBlock && c.RequiredOutcome == "RecoveryCleared") throw new InvalidOperationException("Invalid adversarial matrix case.");
    }
}
