using Lumen.Application.Abstractions;

namespace Lumen.Application.Leaderboards;

public class LeaderboardService
{
    private readonly ILeaderboardRepository _repo;
    private readonly ICurrentUser Context;

    public LeaderboardService(ILeaderboardRepository repo, ICurrentUser context) 
    {
        _repo = repo;
        Context = context;
    }

    // service passes straight through
    // null = the board does not exist for this organisation or is not active
    public Task<IReadOnlyList<LeaderboardEntry>?> GetTopAsync(int leaderboardId, int take)
        => _repo.GetTopAsync(Context.OrganisationId, leaderboardId, take);
}
