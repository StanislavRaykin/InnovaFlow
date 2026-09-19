using Contracts.Identity;
using InnovaFlow.Identity.Data;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Trace;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;
using Identity.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using FluentValidation;
namespace Identity.Endpoints;

public static class Registration
{
    public static RouteGroupBuilder MapRegistration(this RouteGroupBuilder group)
    {
        group.MapPost("/signup", HandleRegistration)
        .WithName("signin")
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

            ApplicationUser user = new ApplicationUser()
            {
                UserName = request.FullName,
                Email = request.Email
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
    public RegistrationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Имейлът е задължителен.")
            .EmailAddress().WithMessage("Невалиден имейл адрес.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Паролата е задължителна.")
            .MinimumLength(8).WithMessage("Паролата трябва да съдържа минимум 8 символа");


        RuleFor(x => x.FullName)
    .Matches(@"^.{1,39}$")
        .WithMessage("Потребителското име трябва да бъде между 1 и 39 символа.")
    .Matches(@"^[a-zA-Z0-9-]+$")
        .WithMessage("Потребителското име може да съдържа само латински букви, цифри и тирета.")
    .Matches(@"^[^-]")
        .WithMessage("Потребителското име не може да започва с тире.")
    .Matches(@"[^-]$")
        .WithMessage("Потребителското име не може да завършва с тире.")
    .Matches(@"^(?!.*--).*$")
        .WithMessage("Потребителското име не може да съдържа две последователни тирета.");
    }
}