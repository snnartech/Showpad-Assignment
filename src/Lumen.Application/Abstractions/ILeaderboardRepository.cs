using Lumen.Application.Leaderboards;
using Lumen.Domain.Entities;

namespace Lumen.Application.Abstractions;

public interface ILeaderboardRepository
{
    // null = the board does not exist for this organisation or is not active
    Task<IReadOnlyList<LeaderboardEntry>?> GetTopAsync(int organisationId, int leaderboardId, int take);
    Task<IReadOnlyList<Leaderboard>> GetActiveBoardsForUserAsync(int userId);
    //Task<int> AddPointsAsync(int leaderboardId, int userId, int points); // RD - Commented out to avoid adding points to the leaderboard
    Task<IReadOnlyList<Leaderboard>> ListByOrganisationAsync(int organisationId, CancellationToken ct = default);
    Task<IReadOnlyList<Leaderboard>> ListAllAsync(CancellationToken ct = default);
    /// <summary>Tries to apply a quiz attempt to the leaderboards. Returns true if the attempt was applied successfully, false if it was already applied.</summary>
    /// <param name="IdempotencyKey">The key of the quiz attempt.</param>
    /// <param name="userId">The ID of the user who attempted the quiz.</param>
    /// <param name="quizId">The ID of the quiz that was attempted.</param>
    /// <param name="points">The points earned by the user for the quiz attempt.</param>
    /// <param name="boardIds">The IDs of the leaderboards to apply the attempt to.</param>
    /// <returns>True if the attempt was applied successfully, false if it was already applied.</returns>
    Task<bool> TryApplyAttemptAsync(Guid IdempotencyKey, int userId, int quizId, int points, IReadOnlyList<int> boardIds);
}
