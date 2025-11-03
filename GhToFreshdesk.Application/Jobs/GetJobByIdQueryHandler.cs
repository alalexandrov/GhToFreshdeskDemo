using MediatR;
using GhToFreshdesk.Application.Abstractions;

namespace GhToFreshdesk.Application.Jobs;

public sealed class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto?>
{
    private readonly IJobStore _jobs;

    public GetJobByIdQueryHandler(IJobStore jobs) => _jobs = jobs;

    public Task<JobDto?> Handle(GetJobByIdQuery request, CancellationToken ct)
        => _jobs.GetByIdAsync(request.JobId, ct);
}