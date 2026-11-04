using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lumen.Auth;

/// <summary>Adds org= and user= to every log line of an authenticated request (see log extract A5).</summary>
public sealed class UserLogScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserLogScopeMiddleware> _log;

    public UserLogScopeMiddleware(RequestDelegate next, ILogger<UserLogScopeMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var org = context.User.FindFirst(LumenClaims.Organisation)?.Value;
        var user = context.User.FindFirst(LumenClaims.Subject)?.Value;

        using (_log.BeginScope(new Dictionary<string, object?> { ["org"] = org, ["user"] = user }))
            await _next(context);
    }
}
