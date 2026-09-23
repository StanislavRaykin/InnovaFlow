using System.Net;
using System.Net.Http.Headers;

namespace Client.Auth;

// Adds the JWT to every request sent to the Gateway. If the Gateway answers 401 the token has
// expired or the session was revoked in Redis, so the user is signed out locally too.
public class BearerTokenHandler(TokenStorage storage, JwtAuthenticationStateProvider authState) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await storage.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && token != null)
            await authState.SignOutAsync();

        return response;
    }
}
