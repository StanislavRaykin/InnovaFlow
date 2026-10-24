using Contracts.Identity;
using InnovaFlow.Identity.Data;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Trace;
using static System.Console;
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

public static class ExternalLogin
{
    private static string[] Providers = new[] { "GitHub", "Google" };
    private static string Normalize(string provider) => Providers.First(x => x.Equals(provider, StringComparison.OrdinalIgnoreCase));
       
    public static RouteGroupBuilder MapExternalLogin(this RouteGroupBuilder group)
    {
        group.MapGet("/{provider}/start", HandleLoginStart);
        group.MapGet("/{provider}/callback", HandleLoginCallback);
        group.MapPost("/exchange", HandleExchange);
        

        return group;

    }

    private static async Task<IResult> HandleLoginStart(string provider, string? returnUrl)
    {
        string scheme = Normalize(provider);
        if (scheme == null)
            return Results.NotFound($"Unsupported provider: {provider}");

        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/external/{provider}/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}"
        };
        return Results.Challenge(properties, [scheme]);
    }
    private static async Task<IResult> HandleLoginCallback(string provider, string? returnUrl, HttpContext ctx, UserManager<ApplicationUser> users, ITokenService tokens, IExchangeCodeStore codes, IOptions<ClientOptions> client, CancellationToken ct)
    {
        string scheme = Normalize(provider);
        if (scheme == null)
        {
          return Results.NotFound($"Unsupported provider: {provider}");
        }
            


        var auth = await ctx.AuthenticateAsync("external");
        if (!auth.Succeeded)
        {
           return Results.Redirect($"{client.Value.BaseUrl}/signin?error=external_failed");
        }

        var user = await FindOrCreateAsync(scheme, auth.Principal!, users, ct);
        if (user == null)
        {
           return Results.Redirect($"{client.Value.BaseUrl}/signin?error=no_email");
        }

        var jwt = await tokens.GenerateTokenAsync(user, ct);
        var code = await codes.StoreAsync(jwt, TimeSpan.FromSeconds(60), ct);

        await ctx.SignOutAsync("external");           // clean up the temp cookie
        return Results.Redirect(
            $"{client.Value.BaseUrl}/auth/callback?code={Uri.EscapeDataString(code)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    private static async Task<IResult> HandleExchange(ExchangeRequest request, IExchangeCodeStore codes, CancellationToken ct)
    {
        var response = await codes.ConsumeAsync(request.Code, ct);

        if (response?.Token != null)
        {
          return Results.Ok(response);
        }

        return Results.Problem("Invalid or expired code.", statusCode: 400);
    }

    private record ExchangeRequest(string Code);



    private static async Task<ApplicationUser> FindOrCreateAsync(string provider, ClaimsPrincipal principal, UserManager<ApplicationUser> users, CancellationToken ct)
    {
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.FindFirstValue(ClaimTypes.Name);

        var linkedUser = await users.FindByLoginAsync(provider, providerKey);
        if (linkedUser != null)
        {
            return linkedUser;
        }
           


        var existing = await users.FindByEmailAsync(email);
        if (existing != null)
        {
            await users.AddLoginAsync(existing, new UserLoginInfo(provider, providerKey, provider));
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

        await users.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
        return user;
    }

}



