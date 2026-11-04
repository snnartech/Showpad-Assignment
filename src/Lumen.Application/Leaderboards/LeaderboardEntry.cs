namespace Lumen.Application.Leaderboards;

public class LeaderboardEntry
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Points { get; set; }
}
