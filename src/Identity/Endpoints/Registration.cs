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
namespace Identity.Endpoints;

public static class Registration
{
    public static RouteGroupBuilder MapRegistration(this RouteGroupBuilder group)
    {
        group.MapPost("/signup", HandleRegistration)
        .WithName("signup")
        .AllowAnonymous()
        .Produces<AuthResponse>()
        .Produces(StatusCodes.Status400BadRequest);

        return group;

    }

    private static async Task<IResult> HandleRegistration(RegistrationRequest request, UserManager<ApplicationUser> users, IValidator<RegistrationRequest> validator, ITokenService tokenService)
    {
        var validation = await validator.ValidateAsync(request);
        if (validation.IsValid)
        {

            var existing = await users.FindByEmailAsync(request.Email);
            if (existing != null)
            {
                return Results.BadRequest("Потребител с този имейл вече съществува.");
            }

            // The email doubles as the Identity user name: it is unique and uses only characters
            // Identity allows. The person's real name lives in the Profile.
            var now = DateTimeOffset.UtcNow;
            var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            ApplicationUser user = new ApplicationUser()
            {
                UserName = request.Email,
                Email = request.Email,
                CreatedAt = now,
                Profile = new Profile
                {
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : null,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            };

            var createdResult = await users.CreateAsync(user, request.Password);
            if (!createdResult.Succeeded)
            {
                return Results.BadRequest(createdResult.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
            }

            AuthResponse authResponse = await tokenService.GenerateTokenAsync(user);
            return Results.Ok(authResponse);


        }
        else
        {
            return Results.ValidationProblem(validation.ToDictionary());

        }



    }

}


public class RegistrationRequestValidator : AbstractValidator<RegistrationRequest>
{
    private readonly Regex Uppercase = new Regex(@"\p{Lu}", RegexOptions.Compiled);
    private readonly Regex Lowercase = new Regex(@"\p{Ll}", RegexOptions.Compiled);

    private readonly Regex Digit = new Regex(@"\p{Nd}", RegexOptions.Compiled);

    private readonly Regex Symbol = new Regex(@"[^\p{L}\p{Nd}]", RegexOptions.Compiled);
    public RegistrationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Имейлът е задължителен.")
            .EmailAddress().WithMessage("Невалиден имейл адрес.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Паролата е задължителна.")
            .MinimumLength(8).WithMessage("Паролата трябва да съдържа минимум 8 символа")
            .Matches(Uppercase).WithMessage("Паролата трябва да съдържа поне 1 главна буква")
            .Matches(Lowercase).WithMessage("Паролата трябва да съдържа поне 1 малка буква")
            .Matches(Digit).WithMessage("Паролата трябва да съдържа поне 1 цифра")
            .Matches(Symbol).WithMessage("Паролата трябва да съдържа поне 1 символ");


        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Името е задължително.")
            .MaximumLength(100).WithMessage("Името не може да бъде по-дълго от 100 символа.")
            .Matches(@"^\p{L}+([ '\-]\p{L}+)*$")
                .WithMessage("Името може да съдържа само букви, интервали, тирета и апострофи.");
    }
}