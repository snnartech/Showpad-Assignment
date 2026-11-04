namespace Lumen.Application.Abstractions;

/// <summary>Persists the RSA private key used by the auth service to sign JWTs (RS256).</summary>
public interface ISigningKeyStore
{
    /// <summary>Returns the PEM encoded RSA private key, creating and persisting one if none exists.</summary>
    string LoadOrCreatePrivateKeyPem();
}
