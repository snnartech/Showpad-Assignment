namespace Lumen.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Region { get; set; }
    public DateTime? HireDate { get; set; }
    public bool IsActive { get; set; } = true;
}
