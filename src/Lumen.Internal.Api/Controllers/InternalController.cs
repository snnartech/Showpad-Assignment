using Lumen.Application.Internal;
using Lumen.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Internal.Api.Controllers;

[ApiController]
[Route("internal")]
[Authorize(AuthenticationSchemes = LumenSchemes.InternalKey)]
public class InternalController : ControllerBase
{
    private readonly InternalOperationsService _service;

    public InternalController(InternalOperationsService service) => _service = service;

    [HttpGet("organisations/{subdomain}")]
    public async Task<IActionResult> Organisation(string subdomain, CancellationToken ct)
    {
        var org = await _service.GetOrganisationBySubdomainAsync(subdomain, ct);
        return org is null ? NotFound() : Ok(org);
    }

    [HttpDelete("cache/leaderboards/{leaderboardId:int}")]
    public async Task<IActionResult> InvalidateLeaderboardCache(int leaderboardId, [FromQuery] int organisationId, [FromQuery] int take = 10, CancellationToken ct = default)
    {
        await _service.InvalidateLeaderboardCacheAsync(organisationId, leaderboardId, take, ct);
        return NoContent();
    }
}
