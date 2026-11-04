// Optional: `dotnet run --project src/Lumen.AppHost` starts every Lumen service together under the Aspire
// dashboard (traces/metrics/logs). Running each project on its own with `dotnet run` works too - they then
// send telemetry to the standalone Aspire dashboard from docker-compose.
//
// Infrastructure (SQL Server, Redis, LocalStack SQS, Mailpit, nginx) is NOT started here; use docker compose.
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Lumen_Auth_Api>("lumen-auth");
builder.AddProject<Projects.Lumen_Api>("lumen-api");
builder.AddProject<Projects.Lumen_Admin_Api>("lumen-admin");
builder.AddProject<Projects.Lumen_Scim_Api>("lumen-scim");
builder.AddProject<Projects.Lumen_Internal_Api>("lumen-internal");
builder.AddProject<Projects.Lumen_Queue>("lumen-queue");

builder.Build().Run();
