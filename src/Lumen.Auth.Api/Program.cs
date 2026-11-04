using Lumen.Application;
using Lumen.Auth;
using Lumen.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();   // Aspire: OpenTelemetry, health checks, service discovery

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
builder.Services
    .AddLumenTokenIssuer()
    .AddLumenAuth(builder.Configuration, useLocalSigningKey: true);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

await app.Services.InitializeInfrastructureAsync();   // database, schema, seed data, SQS queues

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseLumenAuth();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
