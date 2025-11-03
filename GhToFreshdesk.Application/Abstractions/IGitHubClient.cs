using GhToFreshdesk.Domain.Users;

namespace GhToFreshdesk.Application.Abstractions;

public interface IGitHubClient
{
    Task<GitHubUser?> GetUserAsync(string login, CancellationToken ct = default);
}