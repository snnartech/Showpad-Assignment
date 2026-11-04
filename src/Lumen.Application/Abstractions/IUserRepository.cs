using Lumen.Domain.Entities;

namespace Lumen.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListByOrganisationAsync(int organisationId, CancellationToken ct = default);
    Task<int> CreateAsync(User user, CancellationToken ct = default);
    Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    Task<UserCredential?> GetCredentialByEmailAsync(string email, CancellationToken ct = default);
}
