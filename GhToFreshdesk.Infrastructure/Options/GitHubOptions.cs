namespace GhToFreshdesk.Infrastructure.Options;

public sealed class GitHubOptions
{
    public string? Token { get; init; }
    public string BaseUrl { get; init; } = "https://api.github.com/";
}