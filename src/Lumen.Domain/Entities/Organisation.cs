namespace Lumen.Domain.Entities;

public class Organisation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? FeatureFlags { get; set; }
}
