namespace GhToFreshdesk.Application.Abstractions;

public interface IJobStore
{
    Task<Guid> EnqueueSyncGithubUserAsync(string tenant, string login, CancellationToken ct = default);
    Task<JobDto?> TryClaimNextDueAsync(string workerId, CancellationToken ct = default);
    
    Task MarkSucceededAsync(Guid jobId, CancellationToken ct = default);
    Task ScheduleRetryAsync(Guid jobId, string error, TimeSpan nextBackoff, CancellationToken ct = default);
    
    Task<JobDto?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task RetryAsync(Guid jobId, CancellationToken ct = default);
}