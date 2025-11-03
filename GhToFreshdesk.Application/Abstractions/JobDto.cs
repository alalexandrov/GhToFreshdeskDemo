namespace GhToFreshdesk.Application.Abstractions;

public sealed record JobDto(
    Guid Id,
    string Tenant,
    string Login,
    JobStatus Status,
    int Attempts,
    int MaxAttempts,
    DateTime AvailableAt,
    DateTime? PickedAt,
    string? WorkerId,
    string? LastError,
    string CorrelationKey
);