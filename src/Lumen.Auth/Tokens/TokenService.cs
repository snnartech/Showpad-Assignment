using System.Security.Claims;
using Lumen.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.Auth.Tokens;

public record AccessToken(string Token, int ExpiresInSeconds);

public interface ITokenService
{
    AccessToken Create(UserCredential user);
}

public sealed class TokenService : ITokenService
{
    private readonly RsaSigningKeyProvider _keys;
    private readonly JwtOptions _options;

    public TokenService(RsaSigningKeyProvider keys, IOptions<JwtOptions> options)
    {
        _keys = keys;
        _options = options.Value;
    }

    public AccessToken Create(UserCredential user)
    {
        var claims = new List<Claim>
        {
            new(LumenClaims.Subject, user.UserId.ToString()),
            new(LumenClaims.Organisation, user.OrganisationId.ToString()),
            new(LumenClaims.Email, user.Email),
            new(LumenClaims.Name, user.DisplayName)
        };
        claims.AddRange(user.Roles
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(r => new Claim(LumenClaims.Role, r)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            SigningCredentials = new SigningCredentials(_keys.SigningKey, SecurityAlgorithms.RsaSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), _options.ExpiryMinutes * 60);
    }
}
