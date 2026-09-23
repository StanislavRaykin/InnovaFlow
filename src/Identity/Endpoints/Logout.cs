using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ServiceDefaults;
using StackExchange.Redis;
namespace Identity.Endpoints;

public static class Logout
{
    public static RouteGroupBuilder MapLogout(this RouteGroupBuilder group)
    {
        group.MapPost("/signout", HandleLogout)
        .WithName("signout")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<IResult> HandleLogout(ClaimsPrincipal user, IConnectionMultiplexer redis)
    {
        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (jti != null)
        {
            await redis.GetDatabase().KeyDeleteAsync(AuthSessionKeys.For(jti));
        }

        return Results.NoContent();
    }
}
