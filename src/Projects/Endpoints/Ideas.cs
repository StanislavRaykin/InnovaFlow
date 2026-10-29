using System.Reflection.Metadata;
using Contracts.Ideas;
using Contracts;
using FluentValidation;
using InnovaFlow.Projects.Data;
using ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
namespace Projects.Endpoints;

public static class Ideas
{
    public static RouteGroupBuilder MapIdeaEndpoints(this RouteGroupBuilder group)
    {

        group.MapPost("/", HandleCreateIdea);
        group.MapGet("/", HandleGetIdeas);
        group.MapGet("{ideaId:guid}", HandleGetIdea);

        return group;

    }

    // the wizard autosaves a draft under the same id, so submitting it promotes
    // that draft instead of creating a second idea next to it
    public static async Task<IResult> HandleCreateIdea(CreateIdeaRequest ideaRequest, ProjectsDbContext db, ICurrentUser currentUser, IValidator<CreateIdeaRequest> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(ideaRequest, ct);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var idea = await db.Ideas.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == ideaRequest.Id, ct);

        if (idea == null)
        {
            idea = new Idea
            {
                Id = ideaRequest.Id,
                OwnerId = currentUser.Id,
                Members = [new IdeaMember { UserId = currentUser.Id, Role = MemberRole.Owner }]
            };
            await db.Ideas.AddAsync(idea, ct);
        }
        else if (idea.OwnerId != currentUser.Id || idea.DeletedAt != null)
        {
            return Results.NotFound();
        }
        else if (idea.Status != IdeaStatus.Draft)
        {
            return Results.Conflict("Идеята вече е създадена.");
        }

        idea.Title = ideaRequest.Title;
        idea.Description = ideaRequest.Description;
        idea.ProblemStatement = ideaRequest.ProblemStatement;
        idea.TargetAudience = ideaRequest.TargetAudience;
        idea.Category = ideaRequest.Category;
        idea.Industry = ideaRequest.Industry;
        idea.Status = IdeaStatus.Analyzing;
        idea.UpdatedAt = DateTimeOffset.Now.ToUniversalTime();

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/ideas/{idea.Id}", idea.Id);

    }

    // the query filter already limits this to ideas the user is a member of
    public static async Task<IResult> HandleGetIdeas(ProjectsDbContext db, CancellationToken ct)
    {
        var ideas = await db.Ideas.AsNoTracking().ToListAsync(ct);
        List<IdeaDto> dtos = ideas.Select(ToIdeaDto).ToList();

        return Results.Json(dtos);
    }

    public static async Task<IResult> HandleGetIdea(Guid ideaId, ProjectsDbContext db)
    {
        var idea = await db.Ideas.FindAsync(ideaId);
        if (idea == null)
        {
            return Results.NotFound();
        }

        return Results.Json(ToIdeaDto(idea));
    }

    private static IdeaDto ToIdeaDto(Idea idea)
    {
        return new IdeaDto(
        Id: idea.Id,
        Title: idea.Title,
         Description: idea.Description ?? "",
         ProblemStatement: idea.ProblemStatement,
         TargetAudience: idea.TargetAudience,
         Category: idea.Category,
         Industry: idea.Industry,
         Status: idea.Status,
         CurrentVersionId: idea.Id,
         CreatedAt: idea.CreatedAt,
         UpdatedAt: idea.UpdatedAt
        );
    }
}
