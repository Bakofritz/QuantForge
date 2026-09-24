using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class NativeRuntimeQualificationArchitectureChecks
{
    public static void Run()
    {
        if (typeof(NativeRuntimeCapabilityManifest).GetProperty(nameof(NativeRuntimeCapabilityManifest.NativeExecutionPerformed)) is null)
            throw new InvalidOperationException("Native qualification must record whether execution actually occurred.");
        if (typeof(NativeRuntimeQualificationResult).GetProperty(nameof(NativeRuntimeQualificationResult.Qualified)) is null)
            throw new InvalidOperationException("Native qualification result must expose an explicit qualification gate.");
        var service = new NativeRuntimeQualificationService();
        var manifest = new NativeRuntimeCapabilityManifest("test", "unknown", false, false, false, false, "fp");
        var result = service.Evaluate(manifest, new[] { new NativeRuntimeQualificationCheck("TEST", true, "structural") });
        if (result.Qualified) throw new InvalidOperationException("Structural evidence must not qualify a runtime without native execution.");
    }
}
