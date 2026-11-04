namespace Lumen.Domain.Entities;

/// <summary>Login data for a user (local/dev credential store used by the auth service).</summary>
public class UserCredential
{
    public int UserId { get; set; }
    public int OrganisationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>Comma separated roles, e.g. "learner,admin".</summary>
    public string Roles { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
