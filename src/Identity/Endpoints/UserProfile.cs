using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Contracts.Identity;
using FluentValidation;
using InnovaFlow.Identity.Data;
using Microsoft.EntityFrameworkCore;
namespace Identity.Endpoints;

public static class UserProfile
{
    public static RouteGroupBuilder MapProfile(this RouteGroupBuilder group)
    {
        group.MapGet("/profile", HandleGet)
        .WithName("profile")
        .RequireAuthorization()
        .Produces<ProfileResponse>()
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/profile", HandleUpdate)
        .WithName("update-profile")
        .RequireAuthorization()
        .Produces<ProfileResponse>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> HandleGet(ClaimsPrincipal principal, AppIdentityDbContext db, CancellationToken ct)
    {
        var user = await LoadUserAsync(principal, db, ct);
        return user == null ? Results.Unauthorized() : Results.Ok(new ProfileResponse
        
        {
             UserId = user.Id,
        Email = user.Email!,
        FirstName = user.Profile?.FirstName,
        LastName = user.Profile?.LastName,
        Bio = user.Profile?.Bio,
        AvatarUrl = user.Profile?.AvatarUrl
        });
    }

    private static async Task<IResult> HandleUpdate(ClaimsPrincipal principal, UpdateProfileRequest request, AppIdentityDbContext db, IValidator<UpdateProfileRequest> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var user = await LoadUserAsync(principal, db, ct);
        if (user == null)
            return Results.Unauthorized();

        var now = DateTimeOffset.UtcNow;

        if (user.Profile == null)
        {
            user.Profile = new Profile { UserId = user.Id, CreatedAt = now };
            db.Profiles.Add(user.Profile);
        }

        user.Profile.FirstName = Clean(request.FirstName);
        user.Profile.LastName = Clean(request.LastName);
        user.Profile.Bio = Clean(request.Bio);
        user.Profile.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new ProfileResponse
        
        {
             UserId = user.Id,
        Email = user.Email!,
        FirstName = user.Profile?.FirstName,
        LastName = user.Profile?.LastName,
        Bio = user.Profile?.Bio,
        AvatarUrl = user.Profile?.AvatarUrl
        });
    }

    private static async Task<ApplicationUser?> LoadUserAsync(ClaimsPrincipal principal, AppIdentityDbContext db, CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return null;

        return await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("Името не може да бъде по-дълго от 100 символа.")
            .Matches(@"^\s*\p{L}+([ '\-]\p{L}+)*\s*$").When(x => !string.IsNullOrWhiteSpace(x.FirstName))
                .WithMessage("Името може да съдържа само букви, интервали, тирета и апострофи.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Фамилията не може да бъде по-дълга от 100 символа.")
            .Matches(@"^\s*\p{L}+([ '\-]\p{L}+)*\s*$").When(x => !string.IsNullOrWhiteSpace(x.LastName))
                .WithMessage("Фамилията може да съдържа само букви, интервали, тирета и апострофи.");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Биографията не може да бъде по-дълга от 2000 символа.");
    }
}
