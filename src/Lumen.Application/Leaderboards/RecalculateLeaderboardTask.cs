using Lumen.Application.Abstractions;
using Lumen.Application.Messages;
using Microsoft.Extensions.Logging;

namespace Lumen.Application.Leaderboards;

/// <summary>
/// Triggered by SQS message "QuizAttemptCompleted" published by Lumen.Api after a passed attempt.
/// NOTE: kept exactly as found in the assignment (Appendix A4) - known bugs intentionally NOT fixed.
/// </summary>
public class RecalculateLeaderboardTask
{
    private readonly ILeaderboardRepository _repo;
    private readonly ICacheService _cache;
    private readonly IEmailSender _email;
    private readonly ILogger<RecalculateLeaderboardTask> _log;

    public RecalculateLeaderboardTask(
        ILeaderboardRepository repo,
        ICacheService cache,
        IEmailSender email,
        ILogger<RecalculateLeaderboardTask> log)
    {
        _repo = repo;
        _cache = cache;
        _email = email;
        _log = log;
    }

   public async Task Execute(QuizAttemptCompleted msg)
{
    var boards = await _repo.GetActiveBoardsForUserAsync(msg.UserId);
    var applied = await _repo.TryApplyAttemptAsync(
        msg.AttemptKey, msg.UserId, msg.QuizId, msg.Points,
        boards.Select(b => b.Id).ToList());

    if (!applied)
        return;   // duplicate delivery; do not add points, do not send a second email

    foreach (var b in boards)
        await _cache.RemoveAsync($"lb:top:{b.OrganisationId}:{b.Id}:10");

    // After the score is committed. Must not block or undo points.
    await _email.SendCompletionAsync(msg.UserId, msg.QuizId, CancellationToken.None);
}
}
