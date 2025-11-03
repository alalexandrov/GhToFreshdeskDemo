namespace GhToFreshdesk.Domain.Users;

public sealed record GitHubUser(
    long Id,
    string Login,
    string? Name,
    string? Company,
    string? Email,
    string? Blog,
    string? Location,
    string? Bio,
    string? AvatarUrl
);