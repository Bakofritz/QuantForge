using QuantForge.Core;

namespace QuantForge.Core.Runtime;

public sealed record NativeRuntimeCapabilityManifest(
    string RuntimeId,
    string Target,
    bool DotNetAvailable,
    bool SqliteAvailable,
    bool AndroidDeviceAvailable,
    bool NativeExecutionPerformed,
    string ManifestFingerprint);

public sealed record NativeRuntimeQualificationCheck(string Name, bool Passed, string Detail);

public sealed record NativeRuntimeQualificationResult(
    NativeRuntimeCapabilityManifest Manifest,
    IReadOnlyList<NativeRuntimeQualificationCheck> Checks,
    bool Qualified,
    string ResultFingerprint);

/// <summary>
/// Explicitly separates capability discovery from qualification. No capability is treated as
/// qualified merely because a project compiles or an adapter exists.
/// </summary>
public sealed class NativeRuntimeQualificationService
{
    public NativeRuntimeQualificationResult Evaluate(
        NativeRuntimeCapabilityManifest manifest,
        IEnumerable<NativeRuntimeQualificationCheck> checks)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(checks);
        var materialized = checks.ToArray();
        var qualified = manifest.NativeExecutionPerformed && materialized.Length > 0 && materialized.All(x => x.Passed);
        var fingerprint = ResearchFingerprint.Sha256(
            $"{manifest.ManifestFingerprint}|{manifest.NativeExecutionPerformed}|{qualified}|" +
            string.Join("||", materialized.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => $"{x.Name}:{x.Passed}:{x.Detail}")));
        return new NativeRuntimeQualificationResult(manifest, materialized, qualified, fingerprint);
    }
}
