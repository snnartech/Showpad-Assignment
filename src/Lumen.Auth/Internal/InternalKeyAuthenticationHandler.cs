using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumen.Auth.Internal;

public class InternalKeyOptions : AuthenticationSchemeOptions
{
    public const string Section = "InternalApi";
    public const string HeaderName = "X-Internal-Key";

    public string Key { get; set; } = string.Empty;
}

/// <summary>Service-to-service authentication for Lumen.Internal.Api: a shared key in the X-Internal-Key header.</summary>
public sealed class InternalKeyAuthenticationHandler : AuthenticationHandler<InternalKeyOptions>
{
    public InternalKeyAuthenticationHandler(
        IOptionsMonitor<InternalKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(InternalKeyOptions.HeaderName, out var provided))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (string.IsNullOrEmpty(Options.Key) || !string.Equals(provided, Options.Key, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal key."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "internal-service")], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
