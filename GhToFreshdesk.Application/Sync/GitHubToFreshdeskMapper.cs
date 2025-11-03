using GhToFreshdesk.Domain.Contacts;
using GhToFreshdesk.Domain.Users;

namespace GhToFreshdesk.Application.Sync;

public static class GitHubToFreshdeskMapper
{
    public static FreshdeskContact Map(GitHubUser gh, string externalId)
        => new (
            Id: 0,
            Name: gh.Name ?? gh.Login,
            Email: gh.Email,
            JobTitle: gh.Company,
            Address: gh.Location,
            TwitterId: null,
            UniqueExternalId: externalId
        );
}