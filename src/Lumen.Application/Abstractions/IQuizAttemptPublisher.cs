using Lumen.Application.Messages;

namespace Lumen.Application.Abstractions;

/// <summary>Publishes the "QuizAttemptCompleted" message to the queue.</summary>
public interface IQuizAttemptPublisher
{
    /// <returns>The queue message id.</returns>
    Task<string> PublishAsync(QuizAttemptCompleted message, CancellationToken ct = default);
}
