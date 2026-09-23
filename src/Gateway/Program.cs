using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using ServiceDefaults;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDiscovery();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// active login sessions written by Identity
builder.AddRedisClient("cache");

// the browser client is served from a different origin
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

//auth: a token is only accepted while its session still exists in Redis
builder.AddJwtAuthentication();
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
{
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var jti = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var redis = ctx.HttpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();

            if (jti != null || !await redis.GetDatabase().KeyExistsAsync(AuthSessionKeys.For(jti)))
            {
                ctx.Fail("The session has ended.");
            }
        }
    };
});

// every route requires a signed-in user unless it sets "AuthorizationPolicy": "anonymous"
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());


var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!").AllowAnonymous();
app.MapReverseProxy();

app.Run();
