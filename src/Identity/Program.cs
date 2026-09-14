using Identity.Extensions;
using Identity.Services;
using InnovaFlow.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.AddIdentityServices();
builder.AddJwtAuthentication();
builder.Services.AddSingleton<SigningKeyProvider>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddOpenApi();


var app = builder.Build();

// migrations
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>()
               .Database.MigrateAsync();
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapEndpoints();
app.MapGet("/well-known/jwks.json", (SigningKeyProvider provider) =>
{
    var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(provider.PublicKey);
    jwk.Use = "sig";
    jwk.Alg = SecurityAlgorithms.RsaSha256;
    return Results.Ok(new { keys = new[] { jwk } });
});

app.Run();

