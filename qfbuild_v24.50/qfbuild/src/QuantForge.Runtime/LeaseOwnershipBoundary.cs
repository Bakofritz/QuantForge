using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record LeaseOwnershipDecision(bool Owned, string JobId, string DeviceId, string Fingerprint, string? StopCode);

/// <summary>Fail-closed lease boundary used immediately before and after controlled work.</summary>
public sealed class LeaseOwnershipBoundary
{
    private readonly IJobLeaseStore _leases;
    public LeaseOwnershipBoundary(IJobLeaseStore leases) => _leases = leases ?? throw new ArgumentNullException(nameof(leases));

    public async Task<LeaseOwnershipDecision> RenewAndVerifyAsync(string jobId, string deviceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId)) throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("Device id is required.", nameof(deviceId));
        if (leaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        var owned = await _leases.RenewAsync(jobId, deviceId, leaseDuration, cancellationToken);
        var stop = owned ? null : "RESEARCH_LEASE_OWNERSHIP_LOST";
        var fp = ResearchFingerprint.Sha256($"{jobId}|{deviceId}|{leaseDuration}|{owned}|{stop}");
        return new(owned, jobId, deviceId, fp, stop);
    }
}
