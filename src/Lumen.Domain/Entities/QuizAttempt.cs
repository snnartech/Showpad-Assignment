namespace Lumen.Domain.Entities;

public class QuizAttempt
{
    public long Id { get; set; }
    public Guid AttemptKey { get; set; }
    public int UserId { get; set; }
    public int QuizId { get; set; }
    public int Score { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; }
}
