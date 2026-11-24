using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ServiceDefaults;
using StackExchange.Redis;
namespace Identity.Endpoints;

public static class Session
{
    public static RouteGroupBuilder MapSession(this RouteGroupBuilder group)
    {
        // The client calls this when it finds a stored token, so it can tell a token that is merely
        // unexpired apart from one whose session still exists (sign-out, Redis restart).
        group.MapGet("/session", HandleSession)
        .WithName("session")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> HandleSession(ClaimsPrincipal user, IConnectionMultiplexer redis)
    {
        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti);

        return jti != null && await redis.GetDatabase().KeyExistsAsync(AuthSessionKeys.For(jti))
            ? Results.NoContent()
            : Results.Unauthorized();
    }
}
