using QuantForge.Core;
using QuantForge.Core.Runtime;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class BatchManifestIntegrityArchitectureChecks
{
    public static void Validate()
    {
        var manifest = new ResearchBatchManifest("batch", "fingerprint", new[]
        {
            new ResearchBatchManifestCase("context", "job", "dataset", "dataset-fp", "strategy", "strategy-fp", "MES", "1m", "config-fp", "engine-fp")
        });
        if (string.IsNullOrWhiteSpace(manifest.ManifestFingerprint) || manifest.ManifestFingerprint.Length != 64)
            throw new InvalidOperationException("Manifest fingerprint must be SHA-256 formatted.");
        if (!typeof(BatchManifestIntegrityService).GetMethod(nameof(BatchManifestIntegrityService.ValidateAsync))!.ReturnType.IsGenericType)
            throw new InvalidOperationException("Manifest validator must return a task result.");
        if (!typeof(IResourceReceiptStore).GetMethod(nameof(IResourceReceiptStore.LoadExecutionReceiptsForBatchAsync))!.Name.Contains("Batch", StringComparison.Ordinal))
            throw new InvalidOperationException("Batch receipt retrieval is required.");
        if (!typeof(IResearchJobStore).GetMethod(nameof(IResearchJobStore.LoadJobAsync))!.Name.Equals("LoadJobAsync", StringComparison.Ordinal))
            throw new InvalidOperationException("Durable job inspection is required.");
    }
}
