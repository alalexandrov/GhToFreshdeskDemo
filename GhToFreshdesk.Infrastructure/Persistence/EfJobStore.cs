using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace GhToFreshdesk.Infrastructure.Persistence;

public sealed class EfJobStore : IJobStore
{
    private readonly AppDbContext _db;

    public EfJobStore(AppDbContext db) => _db = db;
    
    private static bool IsTerminal(Job j)
        => j.Status == JobStatus.Succeeded || j.Status == JobStatus.DeadLetter;

    private static void Touch(Job j, DateTime now)
    {
        j.UpdatedAt = now;
        j.Version++;
    }

    private static void ResetToPending(Job j, DateTime now)
    {
        j.Status = JobStatus.Pending;
        j.Attempts = 0;
        j.AvailableAt = now;
        j.LastError = null;
        j.PickedAt = null;
        j.WorkerId = null;
        Touch(j, now);
    }

    private static void ClaimForProcessing(Job j, string workerId, DateTime now)
    {
        j.Status = JobStatus.Processing;
        j.PickedAt = now;
        j.WorkerId = workerId;
        Touch(j, now);
    }

    private static void MarkSucceeded(Job j, DateTime now)
    {
        j.Status = JobStatus.Succeeded;
        Touch(j, now);
    }

    private static void ScheduleRetryOrDeadLetter(Job j, string? error, TimeSpan backoff, DateTime now)
    {
        j.Attempts += 1;
        j.LastError = error.Truncate();

        if (j.Attempts >= j.MaxAttempts)
        {
            j.Status = JobStatus.DeadLetter;
            Touch(j, now);
            return;
        }

        j.Status = JobStatus.Pending;
        j.AvailableAt = now + backoff;
        Touch(j, now);
    }

    private Task<Job?> LoadAsync(Guid jobId, CancellationToken ct)
        => _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);

    private static string MakeCorrelationKey(string tenant, string login)
        => $"{tenant}|{login}";

    private static JobDto ToDto(Job j) =>
        new(
            j.Id, j.Tenant, j.Login, j.Status,
            j.Attempts, j.MaxAttempts, j.AvailableAt, j.PickedAt, j.WorkerId,
            j.LastError, j.CorrelationKey
        );

    public async Task<Guid> EnqueueSyncGithubUserAsync(string tenant, string login, CancellationToken ct = default)
    {
        var key = MakeCorrelationKey(tenant, login);
        var now = DateTime.UtcNow;
        
        var existing = await _db.Jobs.FirstOrDefaultAsync(j => j.CorrelationKey == key, ct);

        if (existing is not null)
        {
            if (!IsTerminal(existing))
                return existing.Id;

            ResetToPending(existing, now);
            await _db.SaveChangesAsync(ct);
            return existing.Id;
        }
        
        var job = new Job
        {
            Tenant = tenant,
            Login = login,
            Status = JobStatus.Pending,
            Attempts = 0,
            MaxAttempts = 5,
            AvailableAt = now,
            CorrelationKey = key,
            Version = 0
        };

        _db.Jobs.Add(job);

        try
        {
            await _db.SaveChangesAsync(ct);
            return job.Id;
        }
        catch (DbUpdateException)
        {
            var winner = await _db.Jobs
                .AsNoTracking()
                .Where(j => j.CorrelationKey == key)
                .Select(j => new { j.Id })
                .FirstOrDefaultAsync(ct);

            if (winner is not null) return winner.Id;
            throw;
        }
    }

    public async Task<JobDto?> TryClaimNextDueAsync(string workerId, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var now = DateTime.UtcNow;
            var candidate = await _db.Jobs
                .AsTracking()
                .Where(j => j.Status == JobStatus.Pending && j.AvailableAt <= now)
                .OrderBy(j => j.AvailableAt).ThenBy(j => j.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (candidate is null) return null;
            
            ClaimForProcessing(candidate, workerId, now);

            try
            {
                await _db.SaveChangesAsync(ct);
                return ToDto(candidate);
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.Entry(candidate).State = EntityState.Detached;
            }
        }

        return null;
    }

    public async Task MarkSucceededAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await LoadAsync(jobId, ct);
        
        if (job is null) return;
        
        MarkSucceeded(job, DateTime.UtcNow);
        
        await _db.SaveChangesAsync(ct);
    }

    public async Task ScheduleRetryAsync(Guid jobId, string error, TimeSpan nextBackoff, CancellationToken ct = default)
    {
        var job = await LoadAsync(jobId, ct);
        
        if (job is null) return;
        
        ScheduleRetryOrDeadLetter(job, error, nextBackoff, DateTime.UtcNow);
        
        await _db.SaveChangesAsync(ct);
    }

    public async Task<JobDto?> GetByIdAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        return job is null ? null : ToDto(job);
    }

    public async Task RetryAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await LoadAsync(jobId, ct);
        
        if (job is null) return;
        
        ResetToPending(job, DateTime.UtcNow);
        
        await _db.SaveChangesAsync(ct);
    }
}