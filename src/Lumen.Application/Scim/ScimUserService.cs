using Lumen.Application.Abstractions;
using Lumen.Domain.Entities;

namespace Lumen.Application.Scim;

public class ScimUserService
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _current;

    public ScimUserService(IUserRepository users, ICurrentUser current)
    {
        _users = users;
        _current = current;
    }

    public async Task<ScimListResponse> ListAsync(string? userNameFilter, CancellationToken ct = default)
    {
        var users = await _users.ListByOrganisationAsync(_current.OrganisationId, ct);
        var resources = users
            .Where(u => userNameFilter is null || string.Equals(u.Email, userNameFilter, StringComparison.OrdinalIgnoreCase))
            .Select(ToScim)
            .ToList();
        return new ScimListResponse { TotalResults = resources.Count, ItemsPerPage = resources.Count, Resources = resources };
    }

    public async Task<ScimUser?> GetAsync(int id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null || user.OrganisationId != _current.OrganisationId ? null : ToScim(user);
    }

    /// <returns>null when the userName (email) already exists.</returns>
    public async Task<ScimUser?> CreateAsync(ScimUser request, CancellationToken ct = default)
    {
        if (await _users.GetByEmailAsync(request.UserName, ct) is not null)
            return null;

        var user = new User
        {
            OrganisationId = _current.OrganisationId,
            Email = request.UserName,
            DisplayName = request.DisplayName ?? request.UserName,
            Department = request.Department,
            Region = request.Region,
            HireDate = DateTime.UtcNow.Date,
            IsActive = request.Active
        };
        user.Id = await _users.CreateAsync(user, ct);
        return ToScim(user);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null || user.OrganisationId != _current.OrganisationId)
            return false;
        await _users.SetActiveAsync(id, false, ct);
        return true;
    }

    private static ScimUser ToScim(User u) => new()
    {
        Id = u.Id.ToString(),
        UserName = u.Email,
        DisplayName = u.DisplayName,
        Active = u.IsActive,
        Department = u.Department,
        Region = u.Region,
        Emails = [new ScimEmail { Value = u.Email, Primary = true }]
    };
}
