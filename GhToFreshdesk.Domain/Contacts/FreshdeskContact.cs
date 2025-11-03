namespace GhToFreshdesk.Domain.Contacts;

public sealed record FreshdeskContact(
    long Id,
    string? Name,
    string? Email,
    string? JobTitle,
    string? Address,
    string? TwitterId,
    string? UniqueExternalId
);