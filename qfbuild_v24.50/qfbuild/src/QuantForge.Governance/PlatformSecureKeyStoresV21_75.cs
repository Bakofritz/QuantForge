namespace QuantForge.Governance;

/// <summary>Explicit platform adapter points. Production implementations must delegate key material to OS-protected storage.</summary>
public sealed class AndroidKeystoreAdapter : ISecureKeyStore
{
    public bool IsAvailable { get; }
    public AndroidKeystoreAdapter(bool nativeBridgeConfigured = false) => IsAvailable = nativeBridgeConfigured;
    public void Store(string alias, byte[] keyMaterial) => RequireAvailable();
    public byte[]? Retrieve(string alias) { RequireAvailable(); return null; }
    public void Delete(string alias) => RequireAvailable();
    private void RequireAvailable() { if (!IsAvailable) throw new PlatformNotSupportedException("ANDROID_KEYSTORE_BRIDGE_UNAVAILABLE"); }
}

public sealed class WindowsProtectedKeyStoreAdapter : ISecureKeyStore
{
    public bool IsAvailable { get; }
    public WindowsProtectedKeyStoreAdapter(bool nativeBridgeConfigured = false) => IsAvailable = nativeBridgeConfigured;
    public void Store(string alias, byte[] keyMaterial) => RequireAvailable();
    public byte[]? Retrieve(string alias) { RequireAvailable(); return null; }
    public void Delete(string alias) => RequireAvailable();
    private void RequireAvailable() { if (!IsAvailable) throw new PlatformNotSupportedException("WINDOWS_PROTECTED_KEYSTORE_BRIDGE_UNAVAILABLE"); }
}
