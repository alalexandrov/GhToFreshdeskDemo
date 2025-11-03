using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Application.Jobs;
using GhToFreshdesk.Infrastructure.Http;
using GhToFreshdesk.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using GhToFreshdesk.Infrastructure.Persistence;

namespace GhToFreshdesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        //Database
        var cs = config.GetConnectionString("AppDb");
        Directory.CreateDirectory("./app_data");
        services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseSqlite(cs);
        });
        
        //HTTP Clients
        services.Configure<GitHubOptions>(config.GetSection("GitHub"));
        services.Configure<FreshdeskOptions>(config.GetSection("Freshdesk"));
        
        services.AddHttpClient<IGitHubClient, GitHubHttpClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GhToFreshdesk/1.0");
            if (!string.IsNullOrWhiteSpace(opts.Token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.Token);
        })
        .AddPolicyHandler(TimeoutPolicy())
        .AddPolicyHandler(GetStandardPolicy());

        services.AddHttpClient<IFreshdeskClient, FreshdeskHttpClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<FreshdeskOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.ApiKey))
            {
                var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{opts.ApiKey}:X"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
            }
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddPolicyHandler(TimeoutPolicy())
        .AddPolicyHandler((sp, req) =>
        {
            // Retry everything except POST (avoid duplicates)
            return req.Method != HttpMethod.Post
                ? GetStandardPolicy()
                : Policy.NoOpAsync<HttpResponseMessage>();
        });

        //Stores / Services / Repos
        services.AddScoped<IJobStore, EfJobStore>();
        
        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetStandardPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => (int)r.StatusCode == 429)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

    private static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy()
        => Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(20));
}