using Contracts.Identity;
using InnovaFlow.Identity.Data;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Trace;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using System.Reflection.Metadata;
using Identity.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using FluentValidation;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Identity.Data.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
namespace Identity.Endpoints;

public static class GithubLogin
{
    public static RouteGroupBuilder MapGithubLogin(this RouteGroupBuilder group)
    {
        group.MapGet("/start", HandleLoginStart);
        group.MapGet("/callback", HandleLoginCallback);
        group.MapGet("/exchange", HandleExchange);

        return group;

    }

    private static async Task<IResult> HandleLoginStart(string? returnUrl)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/github/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}"
        };
        return Results.Challenge(properties, ["GitHub"]);
    }
    private static async Task<IResult> HandleLoginCallback(string? returnUrl, HttpContext ctx, UserManager<ApplicationUser> users, ITokenService tokens, IExchangeCodeStore codes, IOptions<ClientOptions> client, CancellationToken ct)
    {
        var auth = await ctx.AuthenticateAsync("external");
        if (!auth.Succeeded)
            return Results.Redirect($"{client.Value.BaseUrl}/auth/login?error=external_failed");

        var user = await FindOrCreateAsync(auth.Principal!, users, ct);
        if (user == null)
            return Results.Redirect($"{client.Value.BaseUrl}/auth/login?error=no_email");

        var jwt = await tokens.GenerateTokenAsync(user, ct);
        var code = await codes.StoreAsync(jwt, TimeSpan.FromSeconds(60), ct);

        await ctx.SignOutAsync("external");           // clean up the temp cookie
        return Results.Redirect(
            $"{client.Value.BaseUrl}/auth/callback?code={code}&returnUrl={returnUrl}");
    }

    private static async Task<IResult> HandleExchange(string code, IExchangeCodeStore codes, CancellationToken ct)
    {
        var response = await codes.ConsumeAsync(code, ct);
        if (response.IsValid())
        {
          return Results.Ok(response);
        }
        
        return Results.Problem("Invalid or expired code.", statusCode: 400);
    }
    


    private static async Task<ApplicationUser> FindOrCreateAsync(ClaimsPrincipal principal, UserManager<ApplicationUser> users, CancellationToken ct)
    {
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.FindFirstValue(ClaimTypes.Name);

        if (await users.FindByLoginAsync("GitHub", providerKey) is { } linked)
            return linked;


        var existing = await users.FindByEmailAsync(email);
        if (existing != null)
        {
            await users.AddLoginAsync(existing, new UserLoginInfo("GitHub", providerKey, "GitHub"));
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var created = await users.CreateAsync(user);
        if (!created.Succeeded) return null;

        await users.AddLoginAsync(user, new UserLoginInfo("GitHub", providerKey, "GitHub"));
        return user;
    }

}



