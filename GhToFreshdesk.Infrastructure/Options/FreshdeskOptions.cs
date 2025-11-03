namespace GhToFreshdesk.Infrastructure.Options;

public sealed class FreshdeskOptions
{
    public string? ApiKey { get; init; }
    public string DomainSuffix { get; init; } = "freshdesk.com";
}