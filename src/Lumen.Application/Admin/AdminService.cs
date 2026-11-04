using Lumen.Application.Abstractions;
using Lumen.Domain.Entities;

namespace Lumen.Application.Admin;

public class AdminService
{
    private readonly IOrganisationRepository _orgs;
    private readonly IUserRepository _users;
    private readonly ILeaderboardRepository _boards;
    private readonly ICurrentUser _current;

    public AdminService(IOrganisationRepository orgs, IUserRepository users, ILeaderboardRepository boards, ICurrentUser current)
    {
        _orgs = orgs;
        _users = users;
        _boards = boards;
        _current = current;
    }

    public Task<Organisation?> GetOrganisationAsync(CancellationToken ct = default)
        => _orgs.GetByIdAsync(_current.OrganisationId, ct);

    public Task UpdateFeatureFlagsAsync(string? featureFlags, CancellationToken ct = default)
        => _orgs.UpdateFeatureFlagsAsync(_current.OrganisationId, featureFlags, ct);

    public Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken ct = default)
        => _users.ListByOrganisationAsync(_current.OrganisationId, ct);

    public Task<IReadOnlyList<Leaderboard>> ListLeaderboardsAsync(CancellationToken ct = default)
        => _boards.ListByOrganisationAsync(_current.OrganisationId, ct);
}
