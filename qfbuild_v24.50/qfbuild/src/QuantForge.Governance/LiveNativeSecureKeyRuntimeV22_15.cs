namespace QuantForge.Governance;

public enum NativeSecureRuntimeKindV22_15 { AndroidKeystore, WindowsProtectedKeyStore, Unavailable }
public sealed record NativeSecureKeyRuntimeStatusV22_15(NativeSecureRuntimeKindV22_15 Kind, bool Available, string Code);

/// <summary>Production adapter boundary. No software fallback is permitted for protected key operations.</summary>
public interface INativeSecureKeyRuntimeV22_15
{
    NativeSecureKeyRuntimeStatusV22_15 Status { get; }
    byte[] Sign(ReadOnlySpan<byte> data);
}

public sealed class UnavailableNativeSecureKeyRuntimeV22_15 : INativeSecureKeyRuntimeV22_15
{
    public NativeSecureKeyRuntimeStatusV22_15 Status => new(NativeSecureRuntimeKindV22_15.Unavailable, false, "NATIVE_SECURE_RUNTIME_UNAVAILABLE");
    public byte[] Sign(ReadOnlySpan<byte> data) => throw new InvalidOperationException("NATIVE_SECURE_RUNTIME_UNAVAILABLE");
}
