using Dapper;
using Lumen.Application.Abstractions;
using Lumen.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Lumen.Infrastructure.Persistence.Repositories;

internal sealed class QuizRepository : IQuizRepository
{
    private readonly IDbConnectionFactory _db;

    public QuizRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Quiz?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Quiz>(new CommandDefinition(
            "SELECT Id, OrganisationId, Title, PointsOnPass, PassMark FROM Quizzes WHERE Id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<long> InsertAttemptAsync(QuizAttempt attempt, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO QuizAttempts (AttemptKey, UserId, QuizId, Score, Passed, CompletedAt)
            OUTPUT INSERTED.Id
            VALUES (@AttemptKey, @UserId, @QuizId, @Score, @Passed, @CompletedAt)
            """, attempt, cancellationToken: ct));
    }
}
