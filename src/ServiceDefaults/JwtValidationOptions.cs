namespace ServiceDefaults;

public class JwtValidationOptions
{
    public string PublicKey { get; set; } = string.Empty;   // base64
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
