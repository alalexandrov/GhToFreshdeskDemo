using MediatR;
using GhToFreshdesk.Application.Abstractions;

namespace GhToFreshdesk.Application.Jobs;

public sealed class RetryJobCommandHandler : IRequestHandler<RetryJobCommand, bool>
{
    private readonly IJobStore _jobs;

    public RetryJobCommandHandler(IJobStore jobs) => _jobs = jobs;

    public async Task<bool> Handle(RetryJobCommand request, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(request.JobId, ct);
        if (job is null)
            return false;

        await _jobs.RetryAsync(request.JobId, ct);
        return true;
    }
}