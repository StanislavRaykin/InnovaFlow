using InnovaFlow.Identity.Data;
using Microsoft.AspNetCore.Identity;
using static System.Net.Mime.MediaTypeNames;
using Contracts.Identity;
namespace Identity.Services;

public interface ITokenService
{
    public Task<AuthResponse> GenerateTokenAsync(ApplicationUser user, CancellationToken ct = default);
}
