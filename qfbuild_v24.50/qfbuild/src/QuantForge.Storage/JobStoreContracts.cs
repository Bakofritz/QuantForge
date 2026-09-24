using QuantForge.Core;

namespace QuantForge.Storage;

public interface IResearchJobStore
{
    Task SaveJobAsync(ResearchJob job, CancellationToken cancellationToken = default);
    Task<ResearchJob?> LoadJobAsync(string jobId, CancellationToken cancellationToken = default);
    Task SaveCheckpointAsync(ResearchCheckpoint checkpoint, CancellationToken cancellationToken = default);
    Task<ResearchCheckpoint?> LoadLatestCheckpointAsync(string jobId, CancellationToken cancellationToken = default);
    Task RequestCancellationAsync(JobCancellation request, CancellationToken cancellationToken = default);
    Task<JobCancellation?> LoadCancellationAsync(string jobId, CancellationToken cancellationToken = default);
    Task SaveProgressAsync(ResearchProgress progress, CancellationToken cancellationToken = default);
    Task<ResearchProgress?> LoadProgressAsync(string jobId, string operation, CancellationToken cancellationToken = default);
}
