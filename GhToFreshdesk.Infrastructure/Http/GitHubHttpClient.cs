using System.Net.Http.Json;
using System.Text.Json;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Domain.Users;
using Microsoft.Extensions.Logging;
namespace GhToFreshdesk.Infrastructure.Http;
public sealed class GitHubHttpClient : IGitHubClient
{
    private readonly HttpClient _http;
    private readonly ILogger<GitHubHttpClient> _logger;

    public GitHubHttpClient(HttpClient http, ILogger<GitHubHttpClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<GitHubUser?> GetUserAsync(string login, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync($"users/{Uri.EscapeDataString(login)}", ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        await resp.EnsureSuccessWithLoggedBodyAsync("GitHub", _logger, ct);

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (json.ValueKind == JsonValueKind.Undefined)
            return null;

        return new GitHubUser(
            Id: json.GetProperty("id").GetInt64(),
            Login: json.GetProperty("login").GetString()!,
            Name: json.TryGetProperty("name", out var n) ? n.GetString() : null,
            Company: json.TryGetProperty("company", out var c) ? c.GetString() : null,
            Email: json.TryGetProperty("email", out var e) ? e.GetString() : null,
            Blog: json.TryGetProperty("blog", out var b) ? b.GetString() : null,
            Location: json.TryGetProperty("location", out var l) ? l.GetString() : null,
            Bio: json.TryGetProperty("bio", out var bio) ? bio.GetString() : null,
            AvatarUrl: json.TryGetProperty("avatar_url", out var a) ? a.GetString() : null
        );
    }
}