namespace QuantForge.Governance;

public enum NativeSecureRuntimeCapabilityStateV22_85 { Unavailable, Available, RotationRequired, Invalidated }

public sealed record NativeSecureRuntimeCapabilityV22_85(
    NativeSecureRuntimeCapabilityStateV22_85 State,
    string RuntimeIdentity,
    string KeyVersion,
    DateTime ObservedAtUtc)
{
    public bool IsUsable(DateTime nowUtc) =>
        State == NativeSecureRuntimeCapabilityStateV22_85.Available &&
        !string.IsNullOrWhiteSpace(RuntimeIdentity) &&
        !string.IsNullOrWhiteSpace(KeyVersion) &&
        ObservedAtUtc <= nowUtc;
}

public static class NativeSecureRuntimeCapabilityLifecycleGateV22_85
{
    public static bool CanUse(NativeSecureRuntimeCapabilityV22_85 capability, DateTime nowUtc) =>
        capability is not null && capability.IsUsable(nowUtc);

    public static bool CanRotate(NativeSecureRuntimeCapabilityV22_85 capability) =>
        capability is not null &&
        capability.State != NativeSecureRuntimeCapabilityStateV22_85.Invalidated &&
        !string.IsNullOrWhiteSpace(capability.RuntimeIdentity);
}
