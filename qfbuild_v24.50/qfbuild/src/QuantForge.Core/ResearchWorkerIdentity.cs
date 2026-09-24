namespace QuantForge.Core;

/// <summary>Stable identity metadata for a research execution worker; it grants no additional authority.</summary>
public sealed record ResearchWorkerIdentity(string WorkerId, string DeviceId, string RuntimeFingerprint)
{
    public string IdentityFingerprint => ResearchFingerprint.Sha256($"{WorkerId}|{DeviceId}|{RuntimeFingerprint}");
}
