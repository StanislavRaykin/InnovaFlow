using System.Reflection.Metadata;
using Contracts.Ideas;
using Contracts;
using FluentValidation;
using InnovaFlow.Projects.Data;
using ServiceDefaults;
using Microsoft.EntityFrameworkCore;
namespace Projects.Endpoints;

public static class Ideas
{
    public static RouteGroupBuilder MapDraftEndpoints(this RouteGroupBuilder group)
    {

        group.MapPost("/", HandleCreateIdea).Produces
        group.MapGet("/", HandleGetIdeas)
        group.MapGet("{ideaId:guid}", HandleGetIdea);

        return group;

    }

    public static async Task<IResult> HandleCreateIdea(CreateIdeaRequest ideaRequest, ProjectsDbContext db, ICurrentUser currentUser, IValidator<CreateIdeaRequest> validator, CancellationToken ct)
    {
        var validation = validator.Validate(ideaRequest);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        Idea idea = new Idea
        {
            Id = Guid.NewGuid(),
            OwnerId = currentUser.Id,
            Title = ideaRequest.Title,
            ProblemStatement = ideaRequest.ProblemStatement,
            TargetAudience = ideaRequest.TargetAudience,
            Category = ideaRequest.Category,
            Industry = ideaRequest.Industry,
            Status = IdeaStatus.Analyzing
        };

        await db.Ideas.AddAsync(idea, ct);
        await db.SaveChangesAsync(ct);

        return Results.Created($"api/ideas/{idea.Id}", idea.Id);

    }
    
    public static async Task<IResult> HandleGetIdeas(ProjectsDbContext db, ICurrentUser currentUser, CancellationToken ct)
    {
        var ideas = db.Ideas.Where(i => i.OwnerId == currentUser.Id || i.Members.Find(m => m.UserId == currentUser.Id) != null)
        .Include(i => i.Versions)
        .Include(i => i.Conversations)
        .Include(i => i.Files);
        
        
    }

}
