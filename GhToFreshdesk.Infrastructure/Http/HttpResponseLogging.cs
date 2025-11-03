using GhToFreshdesk.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace GhToFreshdesk.Infrastructure.Http;

/// <summary>
/// Shared helper/extension to log HTTP response bodies before throwing the built-in HttpRequestException.
/// </summary>
internal static class HttpResponseLogging
{
    public static async Task EnsureSuccessWithLoggedBodyAsync(
        this HttpResponseMessage resp,
        string service,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (resp.IsSuccessStatusCode)
            return;

        string? body = null;
        try
        {
            body = await resp.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            // ignore read errors
        }

        logger.LogError(
            "{Service} HTTP {StatusCode} {Reason}. Body: {Body}",
            service,
            (int)resp.StatusCode,
            resp.ReasonPhrase,
            body.Truncate());

        resp.EnsureSuccessStatusCode();
    }
}