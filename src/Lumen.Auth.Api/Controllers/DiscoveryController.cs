using Lumen.Auth.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lumen.Auth.Api.Controllers;

/// <summary>Minimal OIDC discovery so the other services can validate tokens without sharing secrets.</summary>
[ApiController]
[Route("auth/.well-known")]
[AllowAnonymous]
public class DiscoveryController : ControllerBase
{
    private readonly RsaSigningKeyProvider _keys;
    private readonly JwtOptions _jwt;

    public DiscoveryController(RsaSigningKeyProvider keys, IOptions<JwtOptions> jwt)
    {
        _keys = keys;
        _jwt = jwt.Value;
    }

    [HttpGet("openid-configuration")]
    public IActionResult Configuration() => Ok(new
    {
        issuer = _jwt.Issuer,
        jwks_uri = $"{Request.Scheme}://{Request.Host}/auth/.well-known/jwks.json",
        id_token_signing_alg_values_supported = new[] { "RS256" }
    });

    [HttpGet("jwks.json")]
    public IActionResult Jwks() => Ok(new { keys = new[] { _keys.GetPublicJwk() } });
}
