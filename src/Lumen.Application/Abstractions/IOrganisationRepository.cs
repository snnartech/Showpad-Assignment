using Lumen.Domain.Entities;

namespace Lumen.Application.Abstractions;

public interface IOrganisationRepository
{
    Task<Organisation?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Organisation?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);
    Task UpdateFeatureFlagsAsync(int id, string? featureFlags, CancellationToken ct = default);
}
