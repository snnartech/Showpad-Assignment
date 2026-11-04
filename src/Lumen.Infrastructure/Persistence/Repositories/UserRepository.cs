using Dapper;
using Lumen.Application.Abstractions;
using Lumen.Domain.Entities;

namespace Lumen.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private const string Columns = "Id, OrganisationId, Email, DisplayName, Department, Region, HireDate, IsActive";

    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            $"SELECT {Columns} FROM Users WHERE Id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            $"SELECT {Columns} FROM Users WHERE Email = @email", new { email }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<User>> ListByOrganisationAsync(int organisationId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var rows = await conn.QueryAsync<User>(new CommandDefinition(
            $"SELECT {Columns} FROM Users WHERE OrganisationId = @organisationId ORDER BY DisplayName",
            new { organisationId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<int> CreateAsync(User user, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            INSERT INTO Users (OrganisationId, Email, DisplayName, Department, Region, HireDate, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@OrganisationId, @Email, @DisplayName, @Department, @Region, @HireDate, @IsActive)
            """, user, cancellationToken: ct));
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Users SET IsActive = @isActive WHERE Id = @id", new { id, isActive }, cancellationToken: ct));
    }

    public async Task<UserCredential?> GetCredentialByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<UserCredential>(new CommandDefinition(
            """
            SELECT u.Id AS UserId, u.OrganisationId, u.Email, u.DisplayName, u.IsActive, c.PasswordHash, c.Roles
            FROM Users u
            JOIN UserCredentials c ON c.UserId = u.Id
            WHERE u.Email = @email
            """, new { email }, cancellationToken: ct));
    }
}
