using Lumen.Application.Abstractions;
using Lumen.Domain.Entities;

namespace Lumen.Application.Internal;

public class InternalOperationsService
{
    private readonly IOrganisationRepository _orgs;
    private readonly ICacheService _cache;

    public InternalOperationsService(IOrganisationRepository orgs, ICacheService cache)
    {
        _orgs = orgs;
        _cache = cache;
    }

    public Task<Organisation?> GetOrganisationBySubdomainAsync(string subdomain, CancellationToken ct = default)
        => _orgs.GetBySubdomainAsync(subdomain, ct);

    public Task InvalidateLeaderboardCacheAsync(int organisationId, int leaderboardId, int take, CancellationToken ct = default)
        => _cache.RemoveAsync($"lb:top:{organisationId}:{leaderboardId}:{take}", ct);
}
