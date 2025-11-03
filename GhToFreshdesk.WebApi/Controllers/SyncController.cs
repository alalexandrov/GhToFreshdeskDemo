using GhToFreshdesk.Application.Sync;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GhToFreshdesk.WebApi.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController(IMediator mediator) : ControllerBase
{
    [HttpPost("github-user")]
    public async Task<IActionResult> SyncGithubUser([FromQuery] string login, [FromQuery] string freshdeskSubdomain, CancellationToken ct)
    {
        var result = await mediator.Send(new SyncGitHubUserCommand(freshdeskSubdomain, login), ct);
        return Ok(result);
    }
}