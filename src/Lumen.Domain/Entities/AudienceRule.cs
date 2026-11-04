namespace Lumen.Domain.Entities;

public class AudienceRule
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>JSON document, e.g. {"department":"Sales","region":"EU"}.</summary>
    public string RuleExpression { get; set; } = "{}";
}
