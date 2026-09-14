using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using InnovaFlow.Identity.Data;
using Contracts.Identity;
using Microsoft.IdentityModel.Tokens;
using static System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
namespace Identity.Services;

public class JwtTokenService(UserManager<ApplicationUser> users, IOptions<JwtOptions> options, SigningKeyProvider provider)  : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    private static readonly JwtSecurityTokenHandler JwtHandler = new();

     public async Task<AuthResponse> GenerateTokenAsync(ApplicationUser user, CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;
        DateTime expires = now.AddMinutes(_options.AccessTokenMinutes);
         
        long unix = ((DateTimeOffset)now).ToUnixTimeSeconds();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                unix.ToString(),
                ClaimValueTypes.Integer64)
        };

        foreach (var role in await users.GetRolesAsync(user))
            claims.Add(new Claim(ClaimTypes.Role, role));

        

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.ToUniversalTime(),
            expires: expires.ToUniversalTime(),
            signingCredentials: provider.Credentials);

        var accessToken = JwtHandler.WriteToken(token);
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            Token = accessToken,
            ExpiresAt = expires
        };

    }
}
