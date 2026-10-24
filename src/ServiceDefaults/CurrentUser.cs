using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace ServiceDefaults;


public interface ICurrentUser
{
    Guid Id { get; }                 // throws if unauthenticated — use in protected endpoints
    Guid? IdOrNull { get; }          // safe anywhere
    bool IsAuthenticated { get; }
    string? Email { get; }
    bool IsInRole(string role);
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? IdOrNull =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id : null;

    public Guid Id => IdOrNull
        ?? throw new UnauthorizedAccessException("No authenticated user.");

    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
