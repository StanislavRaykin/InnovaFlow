namespace Identity.Data.Options;

public class JwtOptions
{

    public string KeyId { get; set; } = "innova_1";
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenDays { get; set; } = 10;
}
