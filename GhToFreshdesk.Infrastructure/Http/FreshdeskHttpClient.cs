using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Domain.Contacts;
using Microsoft.Extensions.Logging;

namespace GhToFreshdesk.Infrastructure.Http;

public sealed class FreshdeskHttpClient : IFreshdeskClient
{
    private readonly HttpClient http;
    private readonly ILogger<FreshdeskHttpClient> _logger;

    public FreshdeskHttpClient(HttpClient http, ILogger<FreshdeskHttpClient> logger)
    {
        this.http = http;
        _logger = logger;
    }

    public async Task<long?> FindContactIdByExternalIdAsync(string tenant, string externalId, CancellationToken ct = default)
    {
        using var resp = await http.GetAsync(
            $"https://{tenant}.freshdesk.com/api/v2/contacts?unique_external_id={Uri.EscapeDataString(externalId)}", 
            ct);
        
        await resp.EnsureSuccessWithLoggedBodyAsync("Freshdesk", _logger, ct);
        
        var arr = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0) 
            return null;
        
        return arr[0].GetProperty("id").GetInt64();
    }
    
    public async Task<long?> FindContactIdByEmailAsync(string tenant, string email, CancellationToken ct = default)
    {
        using var resp = await http.GetAsync(
            $"https://{tenant}.freshdesk.com/api/v2/contacts?email={Uri.EscapeDataString(email)}",
            ct);
        
        await resp.EnsureSuccessWithLoggedBodyAsync("Freshdesk", _logger, ct);
        
        var arr = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
            return null;

        return arr[0].GetProperty("id").GetInt64();
    }

    public async Task<long> CreateContactAsync(string tenant, FreshdeskContact c, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            name = c.Name,
            email = c.Email,
            job_title = c.JobTitle,
            address = c.Address,
            twitter_id = c.TwitterId,
            unique_external_id = c.UniqueExternalId
        });
        
        using var resp = await http.PostAsync($"https://{tenant}.freshdesk.com/api/v2/contacts",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);
        
        await resp.EnsureSuccessWithLoggedBodyAsync("Freshdesk", _logger, ct);
        
        var j = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return j.GetProperty("id").GetInt64();
    }

    public async Task UpdateContactAsync(string tenant, long id, FreshdeskContact c, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            name = c.Name,
            email = c.Email,
            job_title = c.JobTitle,
            address = c.Address,
            twitter_id = c.TwitterId,
            unique_external_id = c.UniqueExternalId
        });
        
        using var resp = await http.PutAsync($"https://{tenant}.freshdesk.com/api/v2/contacts/{id}",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);
        
        await resp.EnsureSuccessWithLoggedBodyAsync("Freshdesk", _logger, ct);
    }
}