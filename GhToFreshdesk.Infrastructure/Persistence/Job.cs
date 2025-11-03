using GhToFreshdesk.Application.Abstractions;

namespace GhToFreshdesk.Infrastructure.Persistence;

public sealed class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Tenant { get; set; } = null!;
    public string Login { get; set; } = null!;

    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;

    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? PickedAt { get; set; }
    public string? WorkerId { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string CorrelationKey { get; set; } = null!;
    public int Version { get; set; }
}