using Lumen.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Lumen.Auth;

/// <summary>ICurrentUser backed by the claims of the authenticated JWT.</summary>
internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public int UserId => ReadInt(LumenClaims.Subject);
    public int OrganisationId => ReadInt(LumenClaims.Organisation);

    private int ReadInt(string claimType)
    {
        var value = _accessor.HttpContext?.User.FindFirst(claimType)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}
