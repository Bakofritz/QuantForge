using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class NativeSecureRuntimeCapabilityLifecycleV22_85ArchitectureChecks
{
    public static void Validate()
    {
        var now = DateTime.UtcNow;
        var unavailable = new NativeSecureRuntimeCapabilityV22_85(NativeSecureRuntimeCapabilityStateV22_85.Unavailable, "", "", now);
        if (NativeSecureRuntimeCapabilityLifecycleGateV22_85.CanUse(unavailable, now)) throw new InvalidOperationException("Secure runtime must fail closed when unavailable.");
        var valid = new NativeSecureRuntimeCapabilityV22_85(NativeSecureRuntimeCapabilityStateV22_85.Available, "runtime-a", "key-1", now);
        if (!NativeSecureRuntimeCapabilityLifecycleGateV22_85.CanUse(valid, now)) throw new InvalidOperationException("Valid capability was rejected.");
        var invalidated = valid with { State = NativeSecureRuntimeCapabilityStateV22_85.Invalidated };
        if (NativeSecureRuntimeCapabilityLifecycleGateV22_85.CanUse(invalidated, now)) throw new InvalidOperationException("Invalidated capability must fail closed.");
    }
}
