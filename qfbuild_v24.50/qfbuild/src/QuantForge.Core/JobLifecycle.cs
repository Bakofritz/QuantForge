using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Core;

public static class ResearchFingerprint
{
    public static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class ResearchJobLifecycle
{
    public static ResearchJob Create(string jobId, string datasetFingerprint, string configurationFingerprint, string engineFingerprint, DateTimeOffset now) =>
        new(jobId, DurableJobState.Queued, datasetFingerprint, configurationFingerprint, engineFingerprint, now, now);

    public static ResearchJob Start(ResearchJob job, DateTimeOffset now) =>
        job.State is DurableJobState.Queued or DurableJobState.Paused or DurableJobState.RecoveryRequired
            ? job with { State = DurableJobState.Running, UpdatedAt = now, FailureCode = null }
            : throw new InvalidOperationException($"Job '{job.JobId}' cannot start from {job.State}.");

    public static ResearchJob Pause(ResearchJob job, DateTimeOffset now) =>
        job.State == DurableJobState.Running ? job with { State = DurableJobState.Paused, UpdatedAt = now } : job;

    public static ResearchJob Complete(ResearchJob job, DateTimeOffset now) =>
        job.State == DurableJobState.Running ? job with { State = DurableJobState.Completed, UpdatedAt = now } : throw new InvalidOperationException("Only running jobs can complete.");

    public static ResearchJob Cancel(ResearchJob job, DateTimeOffset now) =>
        job.State is DurableJobState.Completed or DurableJobState.Failed or DurableJobState.Canceled
            ? job : job with { State = DurableJobState.Canceled, UpdatedAt = now };

    public static ResearchJob Recover(ResearchJob job, DateTimeOffset now) =>
        job.State == DurableJobState.Running ? job with { State = DurableJobState.RecoveryRequired, UpdatedAt = now, FailureCode = "LEASE_EXPIRED" } : job;

    public static ResearchJob Fail(ResearchJob job, string code, DateTimeOffset now) =>
        job with { State = DurableJobState.Failed, UpdatedAt = now, FailureCode = code };

    public static bool CheckpointMatches(ResearchJob job, ResearchCheckpoint checkpoint) =>
        checkpoint.JobId == job.JobId &&
        checkpoint.DatasetFingerprint == job.DatasetFingerprint &&
        checkpoint.ConfigurationFingerprint == job.ConfigurationFingerprint &&
        checkpoint.EngineFingerprint == job.EngineFingerprint;
}
