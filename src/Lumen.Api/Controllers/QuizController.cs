using Lumen.Application.Quizzes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[ApiController]
[Route("api/quiz")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly QuizService _service;

    public QuizController(QuizService service) => _service = service;

    /// <summary>Records a quiz attempt; a passed attempt publishes "QuizAttemptCompleted" to SQS.</summary>
    [HttpPost("attempt")]
    public async Task<IActionResult> Attempt([FromBody] SubmitQuizAttemptRequest request, CancellationToken ct)
    {
        var result = await _service.SubmitAttemptAsync(request, ct);
        return result is null ? NotFound() : Accepted(result);
    }
}
