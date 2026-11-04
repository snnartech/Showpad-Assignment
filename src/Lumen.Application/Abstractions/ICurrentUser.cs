namespace Lumen.Application.Abstractions;

/// <summary>The authenticated caller (populated from the JWT by the Auth project).</summary>
public interface ICurrentUser
{
    int UserId { get; }
    int OrganisationId { get; }
}
