namespace Lumen.Domain.Entities;

public class LeaderboardScore
{
    public long Id { get; set; }
    public int LeaderboardId { get; set; }
    public int UserId { get; set; }
    public int Points { get; set; }
    public DateTime UpdatedAt { get; set; }
}
