using MediatR;
using Microsoft.AspNetCore.Mvc;
using GhToFreshdesk.Application.Jobs;
using Microsoft.AspNetCore.Authorization;

namespace GhToFreshdesk.WebApi.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = "JobsOnly")]
    [HttpPost("sync-github-user")]
    public async Task<IActionResult> Enqueue([FromQuery] string login, [FromQuery] string tenant, CancellationToken ct)
    {
        var result = await mediator.Send(new EnqueueSyncGitHubUserCommand(tenant, login), ct);
        return Accepted(new { jobId = result.JobId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var job = await mediator.Send(new GetJobByIdQuery(id), ct);
        return job is null ? NotFound() : Ok(job);
    }

    [Authorize(Policy = "JobsOnly")]
    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        var ok = await mediator.Send(new RetryJobCommand(id), ct);
        return ok ? Accepted(new { jobId = id, status = "Pending" }) : NotFound();
    }
}