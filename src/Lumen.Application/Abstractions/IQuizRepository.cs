using Lumen.Domain.Entities;

namespace Lumen.Application.Abstractions;

public interface IQuizRepository
{
    Task<Quiz?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<long> InsertAttemptAsync(QuizAttempt attempt, CancellationToken ct = default);
}
