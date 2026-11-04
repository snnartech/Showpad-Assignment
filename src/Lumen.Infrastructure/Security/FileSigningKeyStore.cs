using System.Security.Cryptography;
using Lumen.Application.Abstractions;
using Lumen.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lumen.Infrastructure.Security;

/// <summary>Local replacement for a managed key store (KMS/Secrets Manager): keeps the RSA key in a PEM file.</summary>
internal sealed class FileSigningKeyStore : ISigningKeyStore
{
    private static readonly object Gate = new();
    private readonly string _path;

    public FileSigningKeyStore(IOptions<SigningKeyOptions> options, IHostEnvironment env)
    {
        var configured = options.Value.SigningKeyPath;
        _path = Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
    }

    public string LoadOrCreatePrivateKeyPem()
    {
        lock (Gate)
        {
            if (File.Exists(_path))
                return File.ReadAllText(_path);

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            using var rsa = RSA.Create(2048);
            var pem = rsa.ExportPkcs8PrivateKeyPem();
            File.WriteAllText(_path, pem);
            return pem;
        }
    }
}
