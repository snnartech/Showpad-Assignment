using Lumen.Application.Admin;
using Lumen.Application.Internal;
using Lumen.Application.Leaderboards;
using Lumen.Application.Quizzes;
using Lumen.Application.Scim;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LeaderboardService>();
        services.AddScoped<QuizService>();
        services.AddScoped<AdminService>();
        services.AddScoped<ScimUserService>();
        services.AddScoped<InternalOperationsService>();
        services.AddScoped<RecalculateLeaderboardTask>();
        services.AddScoped<NightlySnapshotJob>();
        return services;
    }
}
