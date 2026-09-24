using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record MultiCaseResourcePolicy(
    TimeSpan MaxCaseDuration,
    TimeSpan MaxBatchDuration,
    TimeSpan HeartbeatInterval,
    int MaxConcurrentCases = 1)
{
    public static MultiCaseResourcePolicy Conservative(TimeSpan leaseDuration)
        => new(
            MaxCaseDuration: leaseDuration,
            MaxBatchDuration: TimeSpan.FromHours(24),
            HeartbeatInterval: TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(1).Ticks, leaseDuration.Ticks / 3)),
            MaxConcurrentCases: 1);

    public void Validate()
    {
        if (MaxCaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(MaxCaseDuration));
        if (MaxBatchDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(MaxBatchDuration));
        if (HeartbeatInterval <= TimeSpan.Zero || HeartbeatInterval >= MaxCaseDuration)
            throw new ArgumentOutOfRangeException(nameof(HeartbeatInterval));
        if (MaxConcurrentCases < 1) throw new ArgumentOutOfRangeException(nameof(MaxConcurrentCases));
    }
}

public sealed record MultiCaseExecutionReceipt(
    string BatchId,
    string ContextId,
    string JobId,
    string WorkerId,
    string WorkerFingerprint,
    MultiCaseItemState State,
    string ResultFingerprint,
    string ReceiptFingerprint,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long ElapsedMilliseconds,
    int HeartbeatRenewals,
    string ResourceStatus,
    string? ErrorCode);

public sealed class ResourceGovernedCaseRunner
{
    private readonly IJobLeaseStore _leases;

    public ResourceGovernedCaseRunner(IJobLeaseStore leases) => _leases = leases ?? throw new ArgumentNullException(nameof(leases));

    public async Task<(string ResultFingerprint, int HeartbeatRenewals, long ElapsedMilliseconds)> RunAsync(
        ResearchContextState context,
        string deviceId,
        TimeSpan leaseDuration,
        MultiCaseResourcePolicy policy,
        Func<CancellationToken, Task<string>> executeAsync,
        CancellationToken cancellationToken)
    {
        policy.Validate();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(policy.MaxCaseDuration);
        var started = DateTimeOffset.UtcNow;
        var renewals = 0;
        Exception? heartbeatFailure = null;
        var heartbeat = HeartbeatLoopAsync(context.Descriptor.Identity.JobId, deviceId, leaseDuration, policy.HeartbeatInterval, timeout.Token, () => renewals++);
        try
        {
            var result = await executeAsync(timeout.Token);
            timeout.Token.ThrowIfCancellationRequested();
            var elapsed = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
            return (result, renewals, elapsed);
        }
        finally
        {
            timeout.Cancel();
            try { await heartbeat; } catch (OperationCanceledException) { } catch (Exception ex) { heartbeatFailure = ex; }
            if (heartbeatFailure is not null && !cancellationToken.IsCancellationRequested) throw heartbeatFailure;
        }
    }

    private async Task HeartbeatLoopAsync(string jobId, string deviceId, TimeSpan leaseDuration, TimeSpan interval, CancellationToken token, Action renewed)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(interval, token);
            if (token.IsCancellationRequested) break;
            if (!await _leases.RenewAsync(jobId, deviceId, leaseDuration, token))
                throw new InvalidOperationException("RESEARCH_LEASE_RENEWAL_FAILED");
            renewed();
        }
    }
}
