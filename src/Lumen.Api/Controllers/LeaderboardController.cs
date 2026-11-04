using Lumen.Application.Leaderboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly LeaderboardService _service;

    public LeaderboardController(LeaderboardService service) => _service = service;

    // Controller excerpt as found in the assignment (Appendix A3): service passes straight through.
    [HttpGet("{leaderboardId:int}/top")]
    [Authorize]
    public async Task<IActionResult> Top(int leaderboardId, int take = 10)
    {
        var result = await _service.GetTopAsync(leaderboardId, take);
        return result?.Count > 0 ? Ok(result) : NotFound(); // RD - Added null check
    }
}
