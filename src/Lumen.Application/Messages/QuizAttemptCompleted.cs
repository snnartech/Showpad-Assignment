namespace Lumen.Application.Messages;

/// <summary>SQS message "QuizAttemptCompleted", published by Lumen.Api after a passed attempt.</summary>
public class QuizAttemptCompleted
{
    public Guid AttemptKey { get; set; }
    public int OrganisationId { get; set; }
    public int UserId { get; set; }
    public int QuizId { get; set; }
    public int Points { get; set; }
}
