namespace Lumen.Application.Abstractions;

public interface IEmailSender
{
    Task SendCompletionAsync(int userId, int quizId, CancellationToken ct = default);
}
