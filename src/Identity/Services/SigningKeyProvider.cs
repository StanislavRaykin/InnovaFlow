using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using System.Text;
namespace Identity.Services;

public sealed class SigningKeyProvider : IDisposable
{
    private readonly RSA _privateRsa;
    private readonly RSA _publicRsa;

    public SigningCredentials Credentials { get; }
    public RsaSecurityKey PublicKey { get; }

    public SigningKeyProvider(IOptions<JwtOptions> options)
    {
        var opts = options.Value;

        _privateRsa = RSA.Create();
        _privateRsa.ImportFromPem(Decode(opts.PrivateKey));

        var signing = new RsaSecurityKey(_privateRsa) { KeyId = opts.KeyId };
        Credentials = new SigningCredentials(signing, SecurityAlgorithms.RsaSha256);

        _publicRsa = RSA.Create();
        _publicRsa.ImportFromPem(Decode(opts.PublicKey));
        PublicKey = new RsaSecurityKey(_publicRsa) { KeyId = opts.KeyId };
    }

    private static string Decode(string base64) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(base64));

    public void Dispose()
    {
        _privateRsa.Dispose();
        _publicRsa.Dispose();
    }
}
