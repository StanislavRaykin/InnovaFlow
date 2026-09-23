using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Client.Auth;

// Builds the signed-in user from the JWT in localStorage. The token's signature is not checked
// here (the browser has no key to check it with); the Gateway validates it on every request,
// and a 401 from the Gateway signs the user out (see BearerTokenHandler).
public class JwtAuthenticationStateProvider(TokenStorage storage) : AuthenticationStateProvider
{
    private const string RoleClaim = "role";
    private const string LongRoleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await storage.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaims(token);
        if (claims == null || IsExpired(claims))
        {
            await storage.RemoveTokenAsync();
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: "email", roleType: RoleClaim);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SignInAsync(string token)
    {
        await storage.SetTokenAsync(token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await storage.RemoveTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static bool IsExpired(List<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        return exp == null
            || !long.TryParse(exp, out var seconds)
            || DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
    }

    private static List<Claim>? ParseClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        try
        {
            using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var claims = new List<Claim>();

            foreach (var property in payload.RootElement.EnumerateObject())
            {
                var type = property.Name == LongRoleClaim ? RoleClaim : property.Name;

                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                        claims.Add(new Claim(type, item.ToString()));
                }
                else
                {
                    claims.Add(new Claim(type, property.Value.ToString()));
                }
            }

            return claims;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(base64);
    }
}
