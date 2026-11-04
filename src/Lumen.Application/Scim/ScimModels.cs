using System.Text.Json.Serialization;

namespace Lumen.Application.Scim;

public static class ScimSchemas
{
    public const string User = "urn:ietf:params:scim:schemas:core:2.0:User";
    public const string ListResponse = "urn:ietf:params:scim:api:messages:2.0:ListResponse";
    public const string Error = "urn:ietf:params:scim:api:messages:2.0:Error";
}

public class ScimEmail
{
    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
    [JsonPropertyName("primary")] public bool Primary { get; set; } = true;
}

public class ScimUser
{
    [JsonPropertyName("schemas")] public string[] Schemas { get; set; } = [ScimSchemas.User];
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("userName")] public string UserName { get; set; } = string.Empty;
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("active")] public bool Active { get; set; } = true;
    [JsonPropertyName("emails")] public List<ScimEmail> Emails { get; set; } = [];
    [JsonPropertyName("department")] public string? Department { get; set; }
    [JsonPropertyName("region")] public string? Region { get; set; }
}

public class ScimListResponse
{
    [JsonPropertyName("schemas")] public string[] Schemas { get; set; } = [ScimSchemas.ListResponse];
    [JsonPropertyName("totalResults")] public int TotalResults { get; set; }
    [JsonPropertyName("startIndex")] public int StartIndex { get; set; } = 1;
    [JsonPropertyName("itemsPerPage")] public int ItemsPerPage { get; set; }
    [JsonPropertyName("Resources")] public List<ScimUser> Resources { get; set; } = [];
}
