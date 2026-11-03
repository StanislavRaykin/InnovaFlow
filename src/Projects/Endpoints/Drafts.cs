using System.Reflection.Metadata;
using Projects.Data;
using Contracts;
using Contracts.Ideas;
using Microsoft.CodeAnalysis;
using InnovaFlow.Projects.Data;
using ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
namespace Projects.Endpoints;

public static class Drafts
{
    public static RouteGroupBuilder MapDraftEndpoints(this RouteGroupBuilder group)
    {
        // group.MapGet("/{draftId:guid}", GetDraftsHandler);
        //group.MapGet("/", ListDraftsHandler);
        group.MapPut("/{draftId:guid}", HandleSaveDraft);

        return group;

    }

    // public static async Task<IResult> ListDraftsHandler(Guid draftId,ProjectsDbContext db, ICurrentUser currentUser, CancellationToken ct)
    // {
    //     var draft = await db.Ideas.FindAsync(draftId);
        
    // }

    
    public static async Task<IResult> HandleSaveDraft(Guid draftId, SaveDraftRequest request, ProjectsDbContext db, ICurrentUser currentUser, IValidator<SaveDraftRequest> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var draft = await db.Ideas.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == draftId, ct);

        if (draft == null)
        {
            draft = new Idea
            {
                Id = draftId,
                OwnerId = currentUser.Id,
                Status = IdeaStatus.Draft,
                Members = [new IdeaMember { UserId = currentUser.Id, Role = MemberRole.Owner }]
            };
            Apply(draft, request);

            await db.Ideas.AddAsync(draft, ct);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/idea/drafts/{draftId}", null);
        }

        if (draft.OwnerId != currentUser.Id || draft.DeletedAt != null)
        {
            return Results.NotFound();
        }

        if (draft.Status != IdeaStatus.Draft)
        {
            return Results.Conflict("Идеята вече не е чернова.");
        }

        Apply(draft, request);

        // the draft is tracked, so only touch UpdatedAt when a field really changed
        if (db.ChangeTracker.HasChanges())
        {
            draft.UpdatedAt = DateTimeOffset.Now.ToUniversalTime();
            await db.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }


    private static void Apply(Idea draft, SaveDraftRequest request)
    {
        draft.Title = request.Title ?? "";
        draft.Description = request.Description;
        draft.ProblemStatement = request.ProblemStatement;
        draft.TargetAudience = request.TargetAudience;
        draft.Category = request.Category;
        draft.Industry = request.Industry;
    }

}


// drafts are saved while the user is still typing, so only the limits apply
public class SaveDraftRequestValidator : AbstractValidator<SaveDraftRequest>
{
    public SaveDraftRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(40).WithMessage("Заглавието не може да бъде по-дълго от 40 символа.");

        RuleFor(x => x.Description)
            .MaximumLength(300).WithMessage("Описанието не може да бъде по-дълго от 300 символа.");

        RuleFor(x => x.ProblemStatement)
            .MaximumLength(300).WithMessage("Проблемът не може да бъде по-дълъг от 300 символа.");

        RuleFor(x => x.TargetAudience)
            .MaximumLength(300).WithMessage("Целевата аудитория не може да бъде по-дълга от 300 символа.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Невалидна категория.");

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Индустрията не може да бъде по-дълга от 100 символа.");
    }
}

public class CreateIdeaRequestValidator : AbstractValidator<CreateIdeaRequest>
{
    public CreateIdeaRequestValidator()
    {
        RuleFor(x => x.ProblemStatement)
            .NotEmpty().WithMessage("Проблемът е задължителен.")
            .MaximumLength(300).WithMessage("Проблемът не може да бъде по-дълъг от 300 символа.");

        RuleFor(x => x.TargetAudience)
            .NotEmpty().WithMessage("Целевата аудитория е задължителна.")
            .MaximumLength(300).WithMessage("Целевата аудитория не може да бъде по-дълга от 300 символа.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заглавието е задължително.")
            .MaximumLength(40).WithMessage("Заглавието не може да бъде по-дълго от 40 символа.");

        // the wizard has no description field yet, so it stays optional
        RuleFor(x => x.Description)
            .MaximumLength(300).WithMessage("Описанието не може да бъде по-дълго от 300 символа.");

        // NotEmpty() would reject Business, since it is the enum's default (0) value.
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Невалидна категория.");

        RuleFor(x => x.Industry)
            .NotEmpty().WithMessage("Индустрията е задължителна.")
            .MaximumLength(100).WithMessage("Индустрията не може да бъде по-дълга от 100 символа.");
    }
}
