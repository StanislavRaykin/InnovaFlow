using Contracts;
using Contracts.AnalysisDtos;
using Analysis.Data;
using Contracts.Messaging;
using InnovaFlow.Analysis.Data;
using MassTransit;

namespace Analysis.Api.Endpoints;

public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/analyses")
                  .WithTags("Analyses")
                  .RequireAuthorization();

        g.MapPost("/", HandleStartAnalysis)
         .WithName("StartAnalysis")
         .WithSummary("Queues an analysis for the worker");

        g.MapGet("/{id:guid}", HandleGetAnalysis)
         .WithName("GetAnalysis")
         .WithSummary("What the client polls with signalR");

        return app;
    }
    
    private static async Task<IResult> HandleStartAnalysis(
        StartAnalysisRequest request,
        IPublishEndpoint publishEndpoint,
        AnalysisDbContext dbContext)
    {
        var analysis = new InnovaFlow.Analysis.Data.Analysis
        {
            Id = Guid.NewGuid(),
            Status = AnalysisStatus.Pending,
            StartedAt = DateTime.UtcNow,
            
        };

        dbContext.Analyses.Add(analysis);
        await dbContext.SaveChangesAsync();

        await publishEndpoint.Publish(new AnalysisRequested
        {
            AnalysisId = analysis.Id,
            
        });

        return Results.Accepted($"/api/analyses/{analysis.Id}", new { analysis.Id });
    }
}
