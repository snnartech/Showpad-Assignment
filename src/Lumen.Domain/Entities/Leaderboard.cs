namespace Lumen.Domain.Entities;

public class Leaderboard
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? AudienceRuleId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
