namespace QuantForge.Governance;

public enum NativeSecurePlatform { Android, Windows, Unknown }
public enum NativeAuthOperation { DeviceBoundAuthentication, IndependentSecondFactor }

public sealed record NativePlatformAdapterStatus(bool Available, NativeSecurePlatform Platform, string Provider, string Code, string Reason);

/// <summary>Platform-native authentication boundary. Implementations must delegate to OS-backed authenticators.</summary>
public interface INativeAuthenticationAdapterV21_95
{
    NativePlatformAdapterStatus Status { get; }
    bool Verify(NativeAuthOperation operation, ReadOnlySpan<byte> challenge);
}

/// <summary>Platform-native non-exportable key boundary.</summary>
public interface INativeSecureKeyAdapterV21_95
{
    NativePlatformAdapterStatus Status { get; }
    byte[] Sign(ReadOnlySpan<byte> payload);
}

public sealed class UnavailableNativeAuthenticationAdapterV21_95 : INativeAuthenticationAdapterV21_95
{
    public NativePlatformAdapterStatus Status => new(false, NativeSecurePlatform.Unknown, "none", "NATIVE_AUTH_UNAVAILABLE", "No platform-native authenticator has been configured.");
    public bool Verify(NativeAuthOperation operation, ReadOnlySpan<byte> challenge) => throw new InvalidOperationException("NATIVE_AUTH_UNAVAILABLE");
}

public sealed class UnavailableNativeSecureKeyAdapterV21_95 : INativeSecureKeyAdapterV21_95
{
    public NativePlatformAdapterStatus Status => new(false, NativeSecurePlatform.Unknown, "none", "NATIVE_KEY_UNAVAILABLE", "No platform-native secure key provider has been configured.");
    public byte[] Sign(ReadOnlySpan<byte> payload) => throw new InvalidOperationException("NATIVE_KEY_UNAVAILABLE");
}

public sealed class AndroidNativeAdapterBoundaryV21_95 : INativeAuthenticationAdapterV21_95, INativeSecureKeyAdapterV21_95
{
    public NativePlatformAdapterStatus Status => new(false, NativeSecurePlatform.Android, "android-adapter-boundary", "ANDROID_NATIVE_ADAPTER_PENDING", "Android Keystore/passkey runtime binding must be supplied by the Android application layer.");
    public bool Verify(NativeAuthOperation operation, ReadOnlySpan<byte> challenge) => throw new InvalidOperationException(Status.Code);
    public byte[] Sign(ReadOnlySpan<byte> payload) => throw new InvalidOperationException(Status.Code);
}

public sealed class WindowsNativeAdapterBoundaryV21_95 : INativeAuthenticationAdapterV21_95, INativeSecureKeyAdapterV21_95
{
    public NativePlatformAdapterStatus Status => new(false, NativeSecurePlatform.Windows, "windows-adapter-boundary", "WINDOWS_NATIVE_ADAPTER_PENDING", "Windows platform authentication/secure-key runtime binding must be supplied by the desktop application layer.");
    public bool Verify(NativeAuthOperation operation, ReadOnlySpan<byte> challenge) => throw new InvalidOperationException(Status.Code);
    public byte[] Sign(ReadOnlySpan<byte> payload) => throw new InvalidOperationException(Status.Code);
}
