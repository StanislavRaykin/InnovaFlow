using Microsoft.EntityFrameworkCore;
using InnovaFlow.Identity.Data;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Identity;
using Identity.Services;
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
}
