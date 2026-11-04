using Lumen.Application.Abstractions;

namespace Lumen.Auth.Tokens;

public sealed class LoginService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    /// <returns>null when the credentials are invalid or the user is inactive.</returns>
    public async Task<AccessToken?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var credential = await _users.GetCredentialByEmailAsync(email, ct);
        if (credential is null || !credential.IsActive || !_hasher.Verify(password, credential.PasswordHash))
            return null;

        return _tokens.Create(credential);
    }
}
