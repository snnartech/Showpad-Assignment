using Dapper;
using Lumen.Application.Abstractions;
using Lumen.Domain.Entities;

namespace Lumen.Infrastructure.Persistence.Repositories;

internal sealed class OrganisationRepository : IOrganisationRepository
{
    private readonly IDbConnectionFactory _db;

    public OrganisationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Organisation?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Organisation>(new CommandDefinition(
            "SELECT Id, Name, Subdomain, FeatureFlags FROM Organisations WHERE Id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<Organisation?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Organisation>(new CommandDefinition(
            "SELECT Id, Name, Subdomain, FeatureFlags FROM Organisations WHERE Subdomain = @subdomain", new { subdomain }, cancellationToken: ct));
    }

    public async Task UpdateFeatureFlagsAsync(int id, string? featureFlags, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Organisations SET FeatureFlags = @featureFlags WHERE Id = @id", new { id, featureFlags }, cancellationToken: ct));
    }
}
