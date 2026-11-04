namespace Lumen.Domain.Entities;

public class Quiz
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PointsOnPass { get; set; }
    public int PassMark { get; set; }
}
