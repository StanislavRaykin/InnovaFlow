using Microsoft.EntityFrameworkCore;
using InnovaFlow.Identity.Data;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Identity;
using Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Identity.Data.Options;
namespace Identity.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddIdentityServices(this IHostApplicationBuilder builder)
    {
        string cs = builder.Configuration.GetConnectionString("InnovaFlow")!;

        builder.Services.AddDataProtection();
        builder.Services.AddDbContext<AppIdentityDbContext>(o =>
    o.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "Identity")));


        builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.PrivateKey), "Jwt:PrivateKey is missing.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.PublicKey), "Jwt:PublicKey is missing.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is missing.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is missing.")
    .ValidateOnStart();

        builder.Services.AddOptions<ClientOptions>()
        .Bind(builder.Configuration.GetSection("Client"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
        }).AddRoles<ApplicationRole>()
          .AddEntityFrameworkStores<AppIdentityDbContext>()
          .AddSignInManager()
          .AddUserManager<UserManager<ApplicationUser>>()
          .AddDefaultTokenProviders();
        return builder;

    }

    public static IHostApplicationBuilder AddGithubAuthentication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthentication()
    .AddCookie("external", o =>       // temporary, only lives across the redirect
    {
        o.Cookie.Name = "innovaflow.external";
        o.Cookie.SameSite = SameSiteMode.Lax;   // Lax is required for the OAuth return
        o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGitHub(o =>
    {
        o.ClientId = builder.Configuration["GH-CLI-ID"]! ?? throw new InvalidOperationException("Github client id is missing");
        o.ClientSecret = builder.Configuration["GH-CLI-SECRET"] ?? throw new InvalidOperationException("Github client secret is missing");;
        o.SignInScheme = "external";
        o.CallbackPath = "/signin-github";
        o.Scope.Add("user:email");               // needed — GitHub hides email otherwise
        o.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");
        o.ClaimActions.MapJsonKey("urn:github:login", "login");
        o.SaveTokens = false;
    });
        return builder;
    }
}
