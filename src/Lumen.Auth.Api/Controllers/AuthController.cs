using Lumen.Auth.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Auth.Api.Controllers;

public record LoginRequest(string Email, string Password);

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly LoginService _login;

    public AuthController(LoginService login) => _login = login;

    /// <summary>Exchanges email + password for an RS256 access token.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromBody] LoginRequest request, CancellationToken ct)
    {
        var token = await _login.LoginAsync(request.Email, request.Password, ct);
        if (token is null) return Unauthorized();

        return Ok(new
        {
            access_token = token.Token,
            token_type = "Bearer",
            expires_in = token.ExpiresInSeconds
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(User.Claims.Select(c => new { c.Type, c.Value }));
}
