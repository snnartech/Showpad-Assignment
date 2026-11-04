using System.Text.RegularExpressions;
using Lumen.Application.Scim;
using Lumen.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Scim.Api.Controllers;

/// <summary>SCIM 2.0 user provisioning (subset: list/filter by userName, get, create, deactivate).</summary>
[ApiController]
[Route("scim/v2/Users")]
[Authorize(Policy = LumenPolicies.Scim)]
public partial class UsersController : ControllerBase
{
    private readonly ScimUserService _service;

    public UsersController(ScimUserService service) => _service = service;

    [HttpGet]
    public async Task<ScimListResponse> List([FromQuery] string? filter, CancellationToken ct)
    {
        string? userName = null;
        if (filter is not null && UserNameFilter().Match(filter) is { Success: true } m)
            userName = m.Groups[1].Value;

        return await _service.ListAsync(userName, ct);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var user = await _service.GetAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ScimUser request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return created is null
            ? Conflict(new { schemas = new[] { ScimSchemas.Error }, status = "409", detail = "userName already exists" })
            : Created($"scim/v2/Users/{created.Id}", created);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await _service.DeactivateAsync(id, ct) ? NoContent() : NotFound();

    [GeneratedRegex("^userName\\s+eq\\s+\"(.+)\"$", RegexOptions.IgnoreCase)]
    private static partial Regex UserNameFilter();
}
