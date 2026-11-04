using Lumen.Application.Abstractions;
using Lumen.Application.Messages;
using Lumen.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Lumen.Application.Quizzes;

public record SubmitQuizAttemptRequest(int QuizId, int Score);

public record QuizAttemptResult(Guid AttemptKey, bool Passed, int Score);

public class QuizService
{
    private readonly IQuizRepository _quizzes;
    private readonly IQuizAttemptPublisher _publisher;
    private readonly ICurrentUser _user;
    private readonly ILogger<QuizService> _log;

    public QuizService(IQuizRepository quizzes, IQuizAttemptPublisher publisher, ICurrentUser user, ILogger<QuizService> log)
    {
        _quizzes = quizzes;
        _publisher = publisher;
        _user = user;
        _log = log;
    }

    /// <returns>null when the quiz does not exist for the caller's organisation.</returns>
    public async Task<QuizAttemptResult?> SubmitAttemptAsync(SubmitQuizAttemptRequest request, CancellationToken ct = default)
    {
        var quiz = await _quizzes.GetByIdAsync(request.QuizId, ct);
        if (quiz is null || quiz.OrganisationId != _user.OrganisationId)
            return null;

        var passed = request.Score >= quiz.PassMark;
        var attempt = new QuizAttempt
        {
            AttemptKey = Guid.NewGuid(),
            UserId = _user.UserId,
            QuizId = quiz.Id,
            Score = request.Score,
            Passed = passed,
            CompletedAt = DateTime.UtcNow
        };
        await _quizzes.InsertAttemptAsync(attempt, ct);

        if (passed)
        {
            var messageId = await _publisher.PublishAsync(new QuizAttemptCompleted
            {
                AttemptKey = attempt.AttemptKey,
                OrganisationId = _user.OrganisationId,
                UserId = _user.UserId,
                QuizId = quiz.Id,
                Points = quiz.PointsOnPass
            }, ct);
            _log.LogInformation("sqs.send messageId={MessageId} attemptKey={AttemptKey} points={Points}",
                messageId, attempt.AttemptKey, quiz.PointsOnPass);
        }

        return new QuizAttemptResult(attempt.AttemptKey, passed, request.Score);
    }
}
