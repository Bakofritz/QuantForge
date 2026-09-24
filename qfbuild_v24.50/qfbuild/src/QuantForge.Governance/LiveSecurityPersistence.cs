using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuantForge.Governance;

public sealed record PersistedLiveSecuritySettings(
    bool PathwayEnabled,
    LiveCommunicationSettings CommunicationSettings,
    string SettingsVersion,
    DateTimeOffset UpdatedAt,
    string SettingsFingerprint);

public interface ILiveSecuritySettingsStore
{
    PersistedLiveSecuritySettings? Load();
    void Save(PersistedLiveSecuritySettings settings);
}

public sealed class EncryptedLiveSecuritySettingsStore : ILiveSecuritySettingsStore
{
    private readonly string _path;
    private readonly byte[] _key;

    public EncryptedLiveSecuritySettingsStore(string path, byte[] encryptionKey)
    {
        _path = path;
        _key = encryptionKey.Length is 16 or 24 or 32 ? encryptionKey.ToArray() : throw new ArgumentException("AES key must be 128, 192, or 256 bits.", nameof(encryptionKey));
    }

    public PersistedLiveSecuritySettings? Load()
    {
        if (!File.Exists(_path)) return null;
        var payload = Convert.FromBase64String(File.ReadAllText(_path));
        if (payload.Length < 28) throw new InvalidDataException("LIVE_SECURITY_SETTINGS_CORRUPT");
        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return JsonSerializer.Deserialize<PersistedLiveSecuritySettings>(plain) ?? throw new InvalidDataException("LIVE_SECURITY_SETTINGS_CORRUPT");
    }

    public void Save(PersistedLiveSecuritySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var plain = JsonSerializer.SerializeToUtf8Bytes(settings);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[plain.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        var payload = nonce.Concat(tag).Concat(cipher).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        File.WriteAllText(_path, Convert.ToBase64String(payload), Encoding.UTF8);
    }
}

public sealed class LiveSecuritySettingsManager
{
    private readonly ILiveSecuritySettingsStore _store;
    private PersistedLiveSecuritySettings _current;

    public LiveSecuritySettingsManager(ILiveSecuritySettingsStore store)
    {
        _store = store;
        _current = store.Load() ?? CreateDefault();
    }

    public PersistedLiveSecuritySettings Current => _current;

    public GovernanceDecision Apply(LiveSecuritySettingChangeRequest request, bool pathwayEnabled, LiveCommunicationSettings communications, LiveTradingSecurityService security)
    {
        var decision = security.AuthorizeSecuritySettingChange(request);
        if (!decision.Allowed) return decision;
        var fingerprint = Fingerprint(pathwayEnabled, communications.SettingsFingerprint);
        _current = new(pathwayEnabled, communications, "v1", request.RequestedAt, fingerprint);
        _store.Save(_current);
        return new(true, "LIVE_SECURITY_SETTINGS_SAVED", "Live security settings were persisted after two-factor authorization.");
    }

    private static PersistedLiveSecuritySettings CreateDefault()
    {
        var communications = LiveCommunicationSettings.ResearchOnly();
        return new(false, communications, "v1", DateTimeOffset.UtcNow, Fingerprint(false, communications.SettingsFingerprint));
    }

    private static string Fingerprint(bool enabled, string communicationFingerprint) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"P:{enabled}|C:{communicationFingerprint}"))).ToLowerInvariant();
}
