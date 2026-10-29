using Contracts;
using Contracts.AnalysisDtos;
using Analysis.Data;
using Contracts.Messaging;
using InnovaFlow.Analysis.Data;
using MassTransit;
using ServiceDefaults;
using Analysis.Api.Clients;

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
         .WithSummary("Queues an analysis for the worker");

        // g.MapGet("/{id:guid}", HandleGetAnalysis)
        //  .WithName("GetAnalysis")
        //  .WithSummary("What the client polls with signalR");

        return app;
    }
    
    private static async Task<IResult> HandleStartAnalysis(
        Guid ideaId,
        IPublishEndpoint publishEndpoint,
        AnalysisDbContext db,
        CancellationToken ct,
        ICurrentUser currentUser,
        IdeasClient ideas)
    {

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
}
