using GhToFreshdesk.Domain.Contacts;

namespace GhToFreshdesk.Application.Abstractions;

public interface IFreshdeskClient
{
    Task<long?> FindContactIdByExternalIdAsync(string tenant, string externalId, CancellationToken ct = default);
    Task<long?> FindContactIdByEmailAsync(string tenant, string email, CancellationToken ct = default);
    Task<long> CreateContactAsync(string tenant, FreshdeskContact payload, CancellationToken ct = default);
    Task UpdateContactAsync(string tenant, long id, FreshdeskContact payload, CancellationToken ct = default);
}