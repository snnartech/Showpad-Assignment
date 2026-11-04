using System.Text.Json;
using Dapper;
using Lumen.Application.Abstractions;
using Lumen.Application.Leaderboards;
using Lumen.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace Lumen.Infrastructure.Persistence.Repositories;

internal sealed class LeaderboardRepository : ILeaderboardRepository
{
    private const string BoardColumns = "Id, OrganisationId, Name, AudienceRuleId, PeriodStart, PeriodEnd";

    private readonly IDbConnectionFactory _db;
    private readonly ICacheService _cache;
    private readonly ILogger<LeaderboardRepository> _log;

    public LeaderboardRepository(IDbConnectionFactory db, ICacheService cache, ILogger<LeaderboardRepository> log)
    {
        _db = db;
        _cache = cache;
        _log = log;
    }

    // Read code as found in the assignment (Appendix A3) - intentionally unchanged:
    // no tenant check on the leaderboard and a cache key that does not include the organisation.
    public async Task<IReadOnlyList<LeaderboardEntry>?> GetTopAsync(int organisationId, int leaderboardId, int take)
    {
        var key = $"lb:top:{organisationId}:{leaderboardId}:{take}";
        var cached = await _cache.GetAsync<List<LeaderboardEntry>>(key);
        if (cached != null) return cached;

        const string sql = @"
            SELECT TOP (@take) u.Id AS UserId, u.DisplayName, s.Points
            FROM   LeaderboardScores s
            JOIN   Users u ON u.Id = s.UserId
            WHERE  s.LeaderboardId = @leaderboardId AND u.OrganisationId = @organisationId
            ORDER  BY s.Points DESC"; // RD - Added organisation ID to the query
        await using var conn = await _db.OpenAsync();
        var rows = (await conn.QueryAsync<LeaderboardEntry>(sql, new { organisationId, leaderboardId, take })).ToList();
        await _cache.SetAsync(key, rows, TimeSpan.FromMinutes(5));
        return rows;
    }

    public async Task<IReadOnlyList<Leaderboard>> GetActiveBoardsForUserAsync(int userId)
    {
        await using var conn = await _db.OpenAsync();
        var organisationId = await conn.ExecuteScalarAsync<int?>(
            "SELECT OrganisationId FROM Users WHERE Id = @userId", new { userId });
        if (organisationId is null) return [];

        var today = DateTime.UtcNow.Date;
        var boards = (await conn.QueryAsync<Leaderboard>(
            $"SELECT {BoardColumns} FROM Leaderboards WHERE OrganisationId = @organisationId AND PeriodStart <= @today AND PeriodEnd >= @today",
            new { organisationId, today })).ToList();

        var result = new List<Leaderboard>();
        foreach (var board in boards)
        {
            if (board.AudienceRuleId is null)
            {
                result.Add(board);
                continue;
            }

            var members = await GetRuleMembersAsync(conn, organisationId.Value, board.AudienceRuleId.Value);
            if (members.Contains(userId))
                result.Add(board);
        }

        _log.LogDebug("GetActiveBoardsForUser user={UserId} -> evaluated {Count} boards", userId, boards.Count);
        return result;
    }

    /*public async Task<int> AddPointsAsync(int leaderboardId, int userId, int points)
    {
        await using var conn = await _db.OpenAsync();
        return await conn.ExecuteAsync(
            "UPDATE LeaderboardScores SET Points = Points + @p, UpdatedAt = SYSUTCDATETIME() " +
            "WHERE LeaderboardId = @b AND UserId = @u",
            new { p = points, b = leaderboardId, u = userId });
    }*/ // RD - Commented out to avoid adding points to the leaderboard

    public async Task<IReadOnlyList<Leaderboard>> ListByOrganisationAsync(int organisationId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var rows = await conn.QueryAsync<Leaderboard>(new CommandDefinition(
            $"SELECT {BoardColumns} FROM Leaderboards WHERE OrganisationId = @organisationId ORDER BY Id",
            new { organisationId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Leaderboard>> ListAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var rows = await conn.QueryAsync<Leaderboard>(new CommandDefinition(
            $"SELECT {BoardColumns} FROM Leaderboards ORDER BY Id", cancellationToken: ct));
        return rows.ToList();
    }

    /// <summary>User ids matching an AudienceRule. Cached in Redis (sg:rule:{org}:{rule}); falls back to SQL on cache trouble.</summary>
    private async Task<HashSet<int>> GetRuleMembersAsync(System.Data.Common.DbConnection conn, int organisationId, int ruleId)
    {
        var key = $"sg:rule:{organisationId}:{ruleId}";
        try
        {
            var cached = await _cache.GetAsync<List<int>>(key);
            if (cached != null) return [.. cached];
        }
        catch (Exception ex)
        {
            _log.LogWarning("Cache error on key {Key}; falling back to SQL: {Message}", key, ex.Message);
        }

        var expression = await conn.ExecuteScalarAsync<string?>(
            "SELECT RuleExpression FROM AudienceRules WHERE Id = @ruleId AND OrganisationId = @organisationId",
            new { ruleId, organisationId });
        var (department, region) = ParseRule(expression);

        var ids = (await conn.QueryAsync<int>(
            """
            SELECT Id FROM Users
            WHERE OrganisationId = @organisationId AND IsActive = 1
              AND (@department IS NULL OR Department = @department)
              AND (@region IS NULL OR Region = @region)
            """, new { organisationId, department, region })).ToList();

        try
        {
            await _cache.SetAsync(key, ids, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _log.LogWarning("Could not cache key {Key}: {Message}", key, ex.Message);
        }

        return [.. ids];
    }

    private static (string? Department, string? Region) ParseRule(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return (null, null);
        using var doc = JsonDocument.Parse(expression);
        string? Get(string name) => doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        return (Get("department"), Get("region"));
    }
    public async Task<bool> TryApplyAttemptAsync(Guid IdempotencyKey, int userId, int quizId, int points, IReadOnlyList<int> boardIds)
    {
        await using var conn = await _db.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO ProcessedQuizAttempts (AttemptKey, UserId, QuizId, Points)
                VALUES (@IdempotencyKey, @userId, @quizId, @points)
                """,
                new { IdempotencyKey, userId, quizId, points }, transaction: tx));
            foreach (var boardId in boardIds)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE LeaderboardScores
                    SET Points = Points + @points, UpdatedAt = SYSUTCDATETIME()
                    WHERE LeaderboardId = @boardId AND UserId = @userId
                    """,
                    new { points, boardId, userId }, transaction: tx));
            }
            await tx.CommitAsync();
            return true; // RD - Added Key and Points in the table
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            await tx.RollbackAsync();
            return false;
        }
    }
}
