using Hangfire;
using Lumen.Application;
using Lumen.Auth;
using Lumen.Infrastructure;
using Lumen.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();   // Aspire: OpenTelemetry, health checks, service discovery

builder.Services
    .AddApplication()
    .AddLumenCurrentUser()                 // needed so the shared application services can be resolved (no HTTP user in a worker)
    .AddInfrastructure(builder.Configuration)
    .AddLumenHangfire(builder.Configuration)   // recurring jobs (SQL Server storage)
    .AddSqsQuizAttemptConsumer();              // SQS "quiz-attempts" -> RecalculateLeaderboardTask

var app = builder.Build();

await app.Services.InitializeInfrastructureAsync();   // database, schema, seed data, SQS queues
app.Services.RegisterRecurringJobs();                 // "nightly-snapshot"

app.UseHangfireDashboard("/hangfire");                // http://localhost:5010/hangfire
app.MapGet("/", () => Results.Redirect("/hangfire"));
app.MapDefaultEndpoints();

app.Run();
