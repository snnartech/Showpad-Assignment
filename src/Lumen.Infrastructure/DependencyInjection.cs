using Amazon.SQS;
using Lumen.Application.Abstractions;
using Lumen.Infrastructure.Caching;
using Lumen.Infrastructure.Email;
using Lumen.Infrastructure.Messaging;
using Lumen.Infrastructure.Options;
using Lumen.Infrastructure.Persistence;
using Lumen.Infrastructure.Persistence.Repositories;
using Lumen.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Lumen.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers every infrastructure implementation: SQL Server (Dapper), Redis, SQS, SMTP, key store.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var sql = configuration.GetConnectionString("Sql")
            ?? throw new InvalidOperationException("ConnectionStrings:Sql is not configured.");
        var redis = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");

        services.Configure<SqsOptions>(configuration.GetSection(SqsOptions.Section));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.Section));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Section));
        services.Configure<SigningKeyOptions>(configuration.GetSection(SigningKeyOptions.Section));

        // SQL Server + Dapper
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(sql));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton(sp => new DatabaseInitializer(
            sql, sp.GetRequiredService<IOptions<SeedOptions>>(), sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<ILogger<DatabaseInitializer>>()));
        services.AddScoped<IOrganisationRepository, OrganisationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redis);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // SQS (LocalStack locally)
        services.AddSingleton<IAmazonSQS>(sp => SqsQueueProvisioner.CreateClient(sp.GetRequiredService<IOptions<SqsOptions>>().Value));
        services.AddSingleton<SqsQueueProvisioner>();
        services.AddSingleton<IQuizAttemptPublisher, SqsQuizAttemptPublisher>();

        // SMTP (Mailpit locally), signing key
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<ISigningKeyStore, FileSigningKeyStore>();

        return services;
    }

    /// <summary>Adds the background service that consumes the quiz-attempts queue (queue worker only).</summary>
    public static IServiceCollection AddSqsQuizAttemptConsumer(this IServiceCollection services)
    {
        services.AddHostedService<SqsQuizAttemptConsumer>();
        return services;
    }

    /// <summary>Creates the database/schema/seed data and the SQS queues. Idempotent; call before the host starts.</summary>
    public static async Task InitializeInfrastructureAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await services.GetRequiredService<DatabaseInitializer>().InitializeAsync(ct);
        await services.GetRequiredService<SqsQueueProvisioner>().EnsureQueuesAsync(ct);
    }
}
