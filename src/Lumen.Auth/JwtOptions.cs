namespace Lumen.Auth;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "lumen-auth";
    public string Audience { get; set; } = "lumen";
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>OIDC-style metadata document published by the auth service; resource APIs fetch the signing keys from it.</summary>
    public string MetadataAddress { get; set; } = "http://localhost:5001/auth/.well-known/openid-configuration";
}
