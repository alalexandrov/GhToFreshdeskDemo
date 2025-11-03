using MediatR;
using GhToFreshdesk.Application.Abstractions;

namespace GhToFreshdesk.Application.Jobs;

public sealed class EnqueueSyncGitHubUserHandler
    : IRequestHandler<EnqueueSyncGitHubUserCommand, EnqueueSyncGitHubUserResult>
{
    private readonly IJobStore _jobs;

    public EnqueueSyncGitHubUserHandler(IJobStore jobs) => _jobs = jobs;

    public async Task<EnqueueSyncGitHubUserResult> Handle(EnqueueSyncGitHubUserCommand request, CancellationToken ct)
    {
        var jobId = await _jobs.EnqueueSyncGithubUserAsync(request.Tenant, request.Login, ct);
        return new EnqueueSyncGitHubUserResult(jobId);
    }
}