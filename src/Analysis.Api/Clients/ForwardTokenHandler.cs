using System.Net.Http.Headers;

namespace Analysis.Api.Clients;

/// <summary>
/// Forwards the caller's bearer token on to Projects.
/// Without this, Analysis.Api calls /api/ideas/{id} anonymously and always gets 401,
/// which surfaces as a confusing "idea not found" on every start-analysis request.
/// </summary>
public sealed class ForwardTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(header)
            && AuthenticationHeaderValue.TryParse(header, out var parsed))
        {
            request.Headers.Authorization = parsed;
        }

        return await base.SendAsync(request, ct);
    }
}
