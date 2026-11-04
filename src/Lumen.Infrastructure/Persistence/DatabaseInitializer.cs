using Dapper;
using Lumen.Application.Abstractions;
using Lumen.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumen.Infrastructure.Persistence;

/// <summary>
/// Creates the Lumen database, schema (Appendix A2) and local seed data. Idempotent and safe when several
/// hosts start at the same time (serialised with an application lock).
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly SeedOptions _seed;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DatabaseInitializer> _log;

    public DatabaseInitializer(string connectionString, IOptions<SeedOptions> seed, IPasswordHasher hasher, ILogger<DatabaseInitializer> log)
    {
        _connectionString = connectionString;
        _seed = seed.Value;
        _hasher = hasher;
        _log = log;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await InitializeOnceAsync(ct);
                return;
            }
            catch (SqlException ex) when (attempt < 40)
            {
                _log.LogWarning(ex, "SQL Server not ready yet; retry {Attempt}/40", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    private async Task InitializeOnceAsync(CancellationToken ct)
    {
        var target = new SqlConnectionStringBuilder(_connectionString);
        var dbName = target.InitialCatalog;
        // Pooling is off for this connection: a session-scoped applock survives a connection being returned to the
        // pool, which would leave the lock held (and every other host waiting) until the process exits.
        var masterBuilder = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master", Pooling = false };

        await using var master = new SqlConnection(masterBuilder.ConnectionString);
        await master.OpenAsync(ct);

        // Wait for the lock for up to LockWaitSeconds; the command timeout must be longer than the lock wait.
        var lockResult = await master.ExecuteScalarAsync<int>(new CommandDefinition(
            $"DECLARE @r INT; EXEC @r = sp_getapplock @Resource = N'lumen-db-init', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = {LockWaitSeconds * 1000}; SELECT @r;",
            commandTimeout: LockWaitSeconds + 30,
            cancellationToken: ct));
        if (lockResult < 0)
            throw new InvalidOperationException("Could not acquire database initialisation lock.");

        try
        {
            // CREATE DATABASE can take a while on SQL Server running under emulation (Apple Silicon).
            await master.ExecuteAsync(new CommandDefinition(
                $"IF DB_ID(@name) IS NULL CREATE DATABASE [{dbName.Replace("]", "]]")}];",
                new { name = dbName }, commandTimeout: 300, cancellationToken: ct));

            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync(ct);

            foreach (var statement in DatabaseScripts.Schema)
                await db.ExecuteAsync(new CommandDefinition(statement, commandTimeout: 120, cancellationToken: ct));

            if (_seed.Enabled)
            {
                var today = DateTime.UtcNow.Date;
                await db.ExecuteAsync(new CommandDefinition(
                    DatabaseScripts.SeedData,
                    new { periodStart = today.AddDays(-30), periodEnd = today.AddYears(1), hireDate = today.AddYears(-1) },
                    commandTimeout: 120, cancellationToken: ct));
                await SeedCredentialsAsync(db, ct);
            }

            _log.LogInformation("Database {Database} is ready", dbName);
        }
        finally
        {
            await ReleaseLockAsync(master);
        }
    }

    private const int LockWaitSeconds = 180;

    private static async Task ReleaseLockAsync(SqlConnection master)
    {
        try
        {
            await master.ExecuteAsync("EXEC sp_releaseapplock @Resource = N'lumen-db-init', @LockOwner = 'Session';");
        }
        catch
        {
            // The connection is closed right after this (no pooling), which releases the lock anyway.
        }
    }

    private async Task SeedCredentialsAsync(SqlConnection db, CancellationToken ct)
    {
        var users = (await db.QueryAsync<(int Id, string Email)>(new CommandDefinition(
            "SELECT u.Id, u.Email FROM dbo.Users u LEFT JOIN dbo.UserCredentials c ON c.UserId = u.Id WHERE c.UserId IS NULL",
            cancellationToken: ct))).ToList();

        foreach (var (id, email) in users)
        {
            var roles = email.StartsWith("admin@", StringComparison.OrdinalIgnoreCase) ? "admin,learner"
                      : email.StartsWith("scim@", StringComparison.OrdinalIgnoreCase) ? "scim"
                      : "learner";
            await db.ExecuteAsync(new CommandDefinition(
                "INSERT INTO dbo.UserCredentials (UserId, PasswordHash, Roles) VALUES (@id, @hash, @roles)",
                new { id, hash = _hasher.Hash(_seed.DefaultPassword), roles }, cancellationToken: ct));
        }
    }
}
