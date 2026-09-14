using Contracts.Identity;
using InnovaFlow.Identity.Data;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Trace;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;
using Identity.Services;
namespace Identity.Endpoints;
public static class Login
{
    public static RouteGroupBuilder MapLogin(this RouteGroupBuilder group)
    {
        group.MapPost("/signin", HandleLogin)
        .WithName("signin")
        .AllowAnonymous()
        .Produces<AuthResponse>()
        .Produces(StatusCodes.Status401Unauthorized);

        return group;

    }
    
    private static async Task<IResult> HandleLogin(LoginRequest request, UserManager<ApplicationUser> users, ITokenService tokenService)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user != null)
        {
            var passwordValid = await users.CheckPasswordAsync(user, request.Password);
            if (passwordValid)
            {
                var authResponse = await tokenService.GenerateTokenAsync(user); // TokenService
                return Results.Ok(new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Token = authResponse.Token,
                    ExpiresAt = authResponse.ExpiresAt
                });
            }
            else
            {
                return Results.Unauthorized();
            }
        }else
        {
            return Results.Unauthorized();
        }
        
        
    }
}
