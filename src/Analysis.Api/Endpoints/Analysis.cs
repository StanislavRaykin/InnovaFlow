using Contracts;
using Analysis.Data;
using Contracts.Messaging;
using InnovaFlow.Analysis.Data;
using MassTransit;
using ServiceDefaults;
using Analysis.Api.Clients;
using Contracts.AnalysisDtos;
using AnalysisRequested = Contracts.AnalysisDtos.AnalysisRequested;
using Microsoft.EntityFrameworkCore;

namespace Analysis.Api.Endpoints;

public static class Analysis
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/analyses")
                  .WithTags("Analyses")
                  .RequireAuthorization();

        g.MapPost("/", HandleStartAnalysis)
         .WithName("StartAnalysis")
         .WithSummary("Queues an analysis for the worker")
         .RequireAuthorization();

        g.MapGet("/{id:guid}", HandleGetAnalysis)
         .WithName("GetAnalysis")
         .WithSummary("What the client polls with signalR")
         .RequireAuthorization();

        return app;
    }
    
    private static async Task<IResult> HandleStartAnalysis(
        Guid ideaId,
        IPublishEndpoint publishEndpoint,
        AnalysisDbContext db,
        CancellationToken ct,
        ICurrentUser currentUser,
        IdeasClient ideas
        ){

        var idea = await ideas.GetAsync(ideaId, ct);
        if (idea == null)
        {
            return Results.NotFound();
        }

        bool isAnalysisRunning = db.Analyses.Any(a => a.IdeaId == ideaId && (a.Status == AnalysisStatus.Pending || a.Status == AnalysisStatus.Running));
        if  (isAnalysisRunning)
        {
            return Results.Conflict("Анализът вече е стартиран");
        }

        var analysis = new InnovaFlow.Analysis.Data.Analysis
        {
            Id = Guid.NewGuid(),
            Status = AnalysisStatus.Pending,
            StartedAt = DateTime.UtcNow,
            IdeaId = ideaId,
            IdeaVersionId = idea.CurrentVersionId,
            CompletedModules = 0,
            TotalModules = 0,
            UserId = currentUser.Id
        };

        db.Analyses.Add(analysis);

        await publishEndpoint.Publish(new AnalysisRequested
        
(analysis.Id,
            idea.Id,
            idea.CurrentVersionId,
            currentUser.Id,
            idea.Title,
            idea.Description,
            idea.ProblemStatement,
            idea.TargetAudience,
            idea.Category,
            idea.Industry), ct
        );

        await db.SaveChangesAsync();


        return Results.Accepted($"/api/analyses/{analysis.Id}", new AnalysisAcceptedDto(analysis.Id, analysis.Status));
    }

    private static async Task<IResult> HandleGetAnalysis(Guid id,
        AnalysisDbContext db,
        CancellationToken ct,
        ICurrentUser currentUser)
    
    {
        var analysis = await db.Analyses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUser.Id, ct);
    if (analysis is null) return Results.NotFound();

    var modules = await db.ModuleRuns
    .Where(r => r.AnalysisId == id)
    .ToListAsync(ct);

var list = AnalysisModules.All.Select(m => new
{
    m.Key, m.Value,
    Status = modules.FirstOrDefault(r => r.Module == m.Key)
}).ToList();

    return Results.Ok(new
    {
        analysis.Id,
        Total = modules.Count,
        Done = modules.Count(m => m.Status is "Completed" or "Failed"),
        Modules = modules
    });
    }
}
