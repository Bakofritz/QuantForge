namespace QuantForge.Governance;

public enum PlatformSecureOperationV22_05 { Authenticate, SignAuthorization, VerifyAuthorization }
public sealed record PlatformIntegrationResultV22_05(bool Success, string Code, string Reason, string Provider);

/// <summary>Production adapter contract. The application target supplies the actual OS implementation.</summary>
public interface IPlatformSecurityRuntimeV22_05
{
    NativeSecurePlatform Platform { get; }
    bool Available { get; }
    PlatformIntegrationResultV22_05 Execute(PlatformSecureOperationV22_05 operation, ReadOnlySpan<byte> payload);
}

public sealed class UnavailablePlatformSecurityRuntimeV22_05 : IPlatformSecurityRuntimeV22_05
{
    public NativeSecurePlatform Platform => NativeSecurePlatform.Unknown;
    public bool Available => false;
    public PlatformIntegrationResultV22_05 Execute(PlatformSecureOperationV22_05 operation, ReadOnlySpan<byte> payload) =>
        new(false, "PLATFORM_SECURITY_UNAVAILABLE", "A native platform security runtime has not been connected.", "none");
}

public sealed class PlatformSecurityReadinessV22_05
{
    public static PlatformIntegrationResultV22_05 RequireAvailable(IPlatformSecurityRuntimeV22_05 runtime, PlatformSecureOperationV22_05 operation, ReadOnlySpan<byte> payload)
    {
        if (!runtime.Available) return new(false, "PLATFORM_SECURITY_UNAVAILABLE", "Native platform security is unavailable; operation denied.", "none");
        return runtime.Execute(operation, payload);
    }
}
