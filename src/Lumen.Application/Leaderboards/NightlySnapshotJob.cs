using Lumen.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Lumen.Application.Leaderboards;

/// <summary>Hangfire recurring job "nightly-snapshot": warms the top-10 cache for every board.</summary>
public class NightlySnapshotJob
{
    private readonly ILeaderboardRepository _repo;
    private readonly ILogger<NightlySnapshotJob> _log;

    public NightlySnapshotJob(ILeaderboardRepository repo, ILogger<NightlySnapshotJob> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task RunAsync()
    {
        var boards = await _repo.ListAllAsync();
        foreach (var b in boards)
            await _repo.GetTopAsync(b.OrganisationId, b.Id, 10);
        _log.LogInformation("nightly-snapshot completed for {Count} boards", boards.Count);
    }
}
