using GhToFreshdesk.Application.Abstractions;
using MediatR;

namespace GhToFreshdesk.Application.Sync;

public sealed class SyncGitHubUserHandler : IRequestHandler<SyncGitHubUserCommand, SyncGitHubUserResult>
{
    private const string Created = "created";
    private const string Updated = "updated";

    private readonly IGitHubClient _gitHub;
    private readonly IFreshdeskClient _freshdesk;

    public SyncGitHubUserHandler(IGitHubClient gitHub, IFreshdeskClient freshdesk)
    {
        _gitHub = gitHub;
        _freshdesk = freshdesk;
    }

    public async Task<SyncGitHubUserResult> Handle(SyncGitHubUserCommand request, CancellationToken ct)
    {
        var gh = await _gitHub.GetUserAsync(request.Login, ct)
                 ?? throw new KeyNotFoundException($"GitHub user '{request.Login}' not found.");
        
        var externalId = ExternalId.ForGitHub(gh.Id);
        var contactId = await TryFindContactAsync(request.Tenant, externalId, gh.Email, ct);
        var payload = GitHubToFreshdeskMapper.Map(gh, externalId);
        
        if (contactId is null)
        {
            var newId = await _freshdesk.CreateContactAsync(request.Tenant, payload, ct);
            return new SyncGitHubUserResult(Created, newId, externalId, gh.Id);
        }

        await _freshdesk.UpdateContactAsync(request.Tenant, contactId.Value, payload, ct);
        return new SyncGitHubUserResult(Updated, contactId.Value, externalId, gh.Id);
    }

    private async Task<long?> TryFindContactAsync(string tenant, string externalId, string? email, CancellationToken ct)
    {
        var contact = await _freshdesk.FindContactIdByExternalIdAsync(tenant, externalId, ct);
        if (contact is not null) return contact;

        if (!string.IsNullOrWhiteSpace(email))
            return await _freshdesk.FindContactIdByEmailAsync(tenant, email!, ct);

        return null;
    }
}