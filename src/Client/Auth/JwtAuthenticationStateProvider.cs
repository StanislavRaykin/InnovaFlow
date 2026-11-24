using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Client.Auth;

// Builds the signed-in user from the JWT in localStorage. The token's signature is not checked
// here (the browser has no key to check it with); the Gateway validates it on every request,
// and a 401 from the Gateway signs the user out (see BearerTokenHandler).
//
// An unexpired token is not enough on its own: the server also needs its session in Redis, which
// disappears on sign-out elsewhere or when Redis restarts. So a token found in storage is checked
// with the server once before the user counts as signed in.
public class JwtAuthenticationStateProvider(TokenStorage storage, IHttpClientFactory httpFactory) : AuthenticationStateProvider, IDisposable
{
    // plain client without BearerTokenHandler; that handler depends on this class
    public const string SessionClientName = "GatewaySession";

    private const string RoleClaim = "role";
    private const string LongRoleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private string? checkedToken;
    private Task<bool>? sessionCheck;
    private Timer? expiryTimer;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await storage.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaims(token);
        if (claims == null || IsExpired(claims) || !await IsSessionActiveAsync(token))
        {
            await ClearAsync();
            return Anonymous;
        }

        ScheduleExpiry(claims);

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: "email", roleType: RoleClaim);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task<string?> GetTokenAsync()
    {
        return await storage.GetTokenAsync();
    }

    public async Task SignInAsync(string token)
    {
        await storage.SetTokenAsync(token);

        // just issued by the server, no need to ask about it
        checkedToken = token;
        sessionCheck = Task.FromResult(true);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private async Task ClearAsync()
    {
        expiryTimer?.Dispose();
        expiryTimer = null;
        checkedToken = null;
        sessionCheck = null;
        await storage.RemoveTokenAsync();
    }

    private Task<bool> IsSessionActiveAsync(string token)
    {
        if (sessionCheck == null || checkedToken != token)
        {
            checkedToken = token;
            sessionCheck = CheckSessionAsync(token);
        }

        return sessionCheck;
    }

    private async Task<bool> CheckSessionAsync(string token)
    {
        try
        {
            var http = httpFactory.CreateClient(SessionClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/session");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await http.SendAsync(request);
            return response.StatusCode != HttpStatusCode.Unauthorized;
        }
        catch (HttpRequestException)
        {
            // server unreachable: don't sign out over a network blip; the next API call
            // still gets a 401 if the session is really gone
            sessionCheck = null;
            return true;
        }
    }

    // signs the user out the moment the token expires, instead of on the next API call
    private void ScheduleExpiry(List<Claim> claims)
    {
        if (expiryTimer != null || !TryGetExpiry(claims, out var expiresAt))
            return;

        var due = expiresAt - DateTimeOffset.UtcNow;
        // the browser's setTimeout fires immediately past ~24.8 days; longer tokens are covered by the check on reload
        if (due > TimeSpan.FromDays(24)) return;

        expiryTimer = new Timer(_ => _ = SignOutAsync(), null, due < TimeSpan.Zero ? TimeSpan.Zero : due, Timeout.InfiniteTimeSpan);
    }

    private static bool IsExpired(List<Claim> claims)
    {
        return !TryGetExpiry(claims, out var expiresAt) || expiresAt <= DateTimeOffset.UtcNow;
    }

    private static bool TryGetExpiry(List<Claim> claims, out DateTimeOffset expiresAt)
    {
        expiresAt = default;
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (exp == null || !long.TryParse(exp, out var seconds))
            return false;

        expiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
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
        catch (Exception)
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

    public void Dispose() => expiryTimer?.Dispose();
}
