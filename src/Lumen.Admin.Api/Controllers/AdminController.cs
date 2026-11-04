using Lumen.Application.Admin;
using Lumen.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Admin.Api.Controllers;

public record UpdateFeatureFlagsRequest(string? FeatureFlags);

[ApiController]
[Route("admin")]
[Authorize(Policy = LumenPolicies.OrgAdmin)]
public class AdminController : ControllerBase
{
    private readonly AdminService _service;

    public AdminController(AdminService service) => _service = service;

    [HttpGet("organisation")]
    public async Task<IActionResult> Organisation(CancellationToken ct)
    {
        var org = await _service.GetOrganisationAsync(ct);
        return org is null ? NotFound() : Ok(org);
    }

    [HttpPut("organisation/feature-flags")]
    public async Task<IActionResult> UpdateFeatureFlags([FromBody] UpdateFeatureFlagsRequest request, CancellationToken ct)
    {
        await _service.UpdateFeatureFlagsAsync(request.FeatureFlags, ct);
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct) => Ok(await _service.ListUsersAsync(ct));

    [HttpGet("leaderboards")]
    public async Task<IActionResult> Leaderboards(CancellationToken ct) => Ok(await _service.ListLeaderboardsAsync(ct));
}
