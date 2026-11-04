using System.Security.Cryptography;
using Lumen.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.Auth.Tokens;

/// <summary>RS256 signing key (loaded from <see cref="ISigningKeyStore"/>) with a stable key id.</summary>
public sealed class RsaSigningKeyProvider
{
    public RsaSigningKeyProvider(ISigningKeyStore store)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(store.LoadOrCreatePrivateKeyPem());

        var publicKey = rsa.ExportSubjectPublicKeyInfo();
        var kid = Base64UrlEncoder.Encode(SHA256.HashData(publicKey)[..12]);

        SigningKey = new RsaSecurityKey(rsa) { KeyId = kid };
    }

    public RsaSecurityKey SigningKey { get; }

    /// <summary>Public JWK for the JWKS endpoint.</summary>
    public object GetPublicJwk()
    {
        var p = SigningKey.Rsa.ExportParameters(false);
        return new
        {
            kty = "RSA",
            use = "sig",
            alg = SecurityAlgorithms.RsaSha256,
            kid = SigningKey.KeyId,
            n = Base64UrlEncoder.Encode(p.Modulus),
            e = Base64UrlEncoder.Encode(p.Exponent)
        };
    }
}
