using Lumen.Application.Abstractions;
using Lumen.Auth.Internal;
using Lumen.Auth.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.Auth;

public static class AuthExtensions
{
    /// <summary>
    /// Authentication (JWT bearer, RS256) + authorization policies for a resource API.
    /// When <paramref name="useLocalSigningKey"/> is true (the auth service itself) the token is validated against the
    /// local key; otherwise the signing keys are discovered from the auth service's metadata document.
    /// </summary>
    public static IServiceCollection AddLumenAuth(
        this IServiceCollection services, IConfiguration configuration, bool useLocalSigningKey = false)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        var jwt = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();

        services.AddLumenCurrentUser();

        var authentication = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        authentication.AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                NameClaimType = LumenClaims.Subject,
                RoleClaimType = LumenClaims.Role
            };

            if (!useLocalSigningKey)
                options.MetadataAddress = jwt.MetadataAddress;
        });

        if (useLocalSigningKey)
        {
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<RsaSigningKeyProvider>((options, keys) =>
                    options.TokenValidationParameters.IssuerSigningKey = keys.SigningKey);
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(LumenPolicies.OrgAdmin, p => p.RequireAuthenticatedUser().RequireRole(LumenRoles.Admin))
            .AddPolicy(LumenPolicies.Scim, p => p.RequireAuthenticatedUser().RequireRole(LumenRoles.Scim));

        return services;
    }

    /// <summary>ICurrentUser backed by the JWT claims of the current HTTP request.</summary>
    public static IServiceCollection AddLumenCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }

    /// <summary>Token issuing (login + RS256 signing). Used by the auth service only.</summary>
    public static IServiceCollection AddLumenTokenIssuer(this IServiceCollection services)
    {
        services.AddSingleton<RsaSigningKeyProvider>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<LoginService>();
        return services;
    }

    /// <summary>Adds the X-Internal-Key scheme used by Lumen.Internal.Api.</summary>
    public static IServiceCollection AddLumenInternalKeyAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddScheme<InternalKeyOptions, InternalKeyAuthenticationHandler>(LumenSchemes.InternalKey, options =>
                options.Key = configuration[$"{InternalKeyOptions.Section}:Key"] ?? string.Empty);
        return services;
    }

    public static IApplicationBuilder UseLumenAuth(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserLogScopeMiddleware>();
        return app;
    }
}
