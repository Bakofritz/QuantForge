using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Storage;

/// <summary>
/// Dependency-light fault-injection harness. It validates that the fault contract is
/// explicit and that injected persistence failures are observable rather than silently
/// converted into success. It does not mutate production policy or enable concurrency.
/// </summary>
public sealed class StorageFaultInjectionHarness
{
    public Task<StorageFaultInjectionResult> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var checks = new List<StorageFaultInjectionCheck>();
        foreach (var point in new[]
        {
            StorageFaultPoint.LeaseAcquire,
            StorageFaultPoint.LeaseRenew,
            StorageFaultPoint.CheckpointWrite,
            StorageFaultPoint.ReceiptWrite,
            StorageFaultPoint.EvidenceWrite,
            StorageFaultPoint.ValidationResultWrite,
            StorageFaultPoint.JobStateWrite
        })
        {
            var injector = new StorageFaultInjector(new StorageFaultPlan(point));
            try
            {
                injector.ThrowIfConfigured(point);
                checks.Add(new(point, StorageFaultDisposition.UnexpectedlySucceeded, "Configured fault point did not throw."));
            }
            catch (StorageFaultInjectedException ex) when (ex.Point == point && ex.Invocation == 1)
            {
                checks.Add(new(point, StorageFaultDisposition.Injected, "Fault was explicitly injected and surfaced with its error code."));
            }
            catch (Exception ex)
            {
                checks.Add(new(point, StorageFaultDisposition.UnexpectedException, ex.GetType().Name));
            }
        }

        var payload = string.Join("|", checks.Select(c => $"{c.Point}:{c.Disposition}:{c.Detail}"));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var passed = checks.All(c => c.Disposition == StorageFaultDisposition.Injected);
        return Task.FromResult(new StorageFaultInjectionResult(
            "QF-STORAGE-FAULT-HARNESS-1", passed, checks.AsReadOnly(), fingerprint, true, false));
    }
    private static StorageFaultPoint[] StandardStorageFaultPoints() => new[]
    {
        StorageFaultPoint.LeaseAcquire, StorageFaultPoint.LeaseRenew, StorageFaultPoint.CheckpointWrite,
        StorageFaultPoint.ReceiptWrite, StorageFaultPoint.EvidenceWrite, StorageFaultPoint.ValidationResultWrite,
        StorageFaultPoint.JobStateWrite
    };

}
