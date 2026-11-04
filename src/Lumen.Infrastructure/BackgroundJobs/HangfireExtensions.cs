using Hangfire;
using Hangfire.SqlServer;
using Lumen.Application.Leaderboards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Infrastructure.BackgroundJobs;

public static class HangfireExtensions
{
    /// <summary>Hangfire (SQL Server storage) + a processing server. Used by the queue worker only.</summary>
    public static IServiceCollection AddLumenHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Sql")
            ?? throw new InvalidOperationException("ConnectionStrings:Sql is not configured.");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));
        services.AddHangfireServer();
        return services;
    }

    public static void RegisterRecurringJobs(this IServiceProvider services)
    {
        var manager = services.GetRequiredService<IRecurringJobManager>();
        manager.AddOrUpdate<NightlySnapshotJob>("nightly-snapshot", job => job.RunAsync(), "0 11 * * *");
    }
}
