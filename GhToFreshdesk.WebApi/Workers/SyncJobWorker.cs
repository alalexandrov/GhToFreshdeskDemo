using MediatR;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Application.Sync;

namespace GhToFreshdesk.WebApi.Workers;

public sealed class SyncJobWorker : BackgroundService
{
    private readonly ILogger<SyncJobWorker> _log;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);

    public SyncJobWorker(ILogger<SyncJobWorker> log, IServiceScopeFactory scopeFactory)
    {
        _log = log;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerId = $"{Environment.MachineName}:{Environment.ProcessId}";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var jobs = scope.ServiceProvider.GetRequiredService<IJobStore>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var job = await jobs.TryClaimNextDueAsync(workerId, stoppingToken);
                if (job is null)
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                    continue;
                }

                _log.LogInformation("Processing job {JobId} ({Tenant}/{Login})", job.Id, job.Tenant, job.Login);

                try
                {
                    await mediator.Send(new SyncGitHubUserCommand(job.Tenant, job.Login), stoppingToken);
                    await jobs.MarkSucceededAsync(job.Id, stoppingToken);
                    _log.LogInformation("Job {JobId} succeeded", job.Id);
                }
                catch (Exception ex)
                {
                    var next = NextBackoff(job.Attempts);
                    await jobs.ScheduleRetryAsync(job.Id, ex.Message, next, stoppingToken);
                    _log.LogWarning(ex, "Job {JobId} failed; retry in {Backoff}", job.Id, next);
                }
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Worker loop error");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private static TimeSpan NextBackoff(int attempts)
    {
        // simple exponential backoff with cap
        var pow = Math.Clamp(attempts, 0, 6);
        var seconds = Math.Min(60, (int)Math.Pow(2, pow) * 2);
        return TimeSpan.FromSeconds(seconds);
    }
}