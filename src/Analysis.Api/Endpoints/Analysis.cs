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

        // Saved module results, each in the same shape the client streams. 404 until the module has completed.
        g.MapGet("/{id:guid}/audience", HandleGetAudience)
         .WithName("GetAudienceAnalysis")
         .WithSummary("Saved audience result, in the same shape the client streams")
         .RequireAuthorization();

        g.MapGet("/{id:guid}/market", HandleGetMarket)
         .WithName("GetMarketAnalysis")
         .WithSummary("Saved market result, in the same shape the client streams")
         .RequireAuthorization();

        g.MapGet("/{id:guid}/innovation", HandleGetInnovation)
         .WithName("GetInnovationAnalysis")
         .WithSummary("Saved innovation result, in the same shape the client streams")
         .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> HandleGetAudience(Guid id,
        AnalysisDbContext db,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (!await OwnsAsync(db, id, currentUser, ct)) return Results.NotFound();

        var audience = await db.AudienceAnalyses.AsNoTracking()
            .Include(a => a.Personas)
            .FirstOrDefaultAsync(a => a.AnalysisId == id, ct);
        if (audience is null) return Results.NotFound();   // module hasn't finished (or failed)

        var scores = await ScoresAsync(db, id, ct);
        var score = scores.GetValueOrDefault(ScoreType.Audience);

        return Results.Ok(new AudienceAnalysisDto(
            Score100(score?.Score),
            score?.Explanation,
            AudienceText.ParseSegments(audience.PrimaryAudience).FirstOrDefault(),
            AudienceText.ParseSegments(audience.SecondaryAudience),
            [.. audience.Personas.Select(p => new PersonaDto(
                p.Name, p.AgeRange, p.Occupation, p.Description, p.Goals, p.PainPoints, p.Behaviors))],
            audience.Description));
    }

    private static async Task<IResult> HandleGetMarket(Guid id,
        AnalysisDbContext db,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (!await OwnsAsync(db, id, currentUser, ct)) return Results.NotFound();

        var market = await db.MarketAnalyses.AsNoTracking().FirstOrDefaultAsync(m => m.AnalysisId == id, ct);
        if (market is null) return Results.NotFound();

        // only the Market module's own competitors; Innovation reads them all
        var competitors = await db.Competitors.AsNoTracking()
            .Where(c => c.AnalysisId == id && c.Source == "Market")
            .ToListAsync(ct);

        var scores = await ScoresAsync(db, id, ct);
        var opportunity = scores.GetValueOrDefault(ScoreType.Market);
        var competition = scores.GetValueOrDefault(ScoreType.Competition);

        return Results.Ok(new MarketAnalysisDto(
            market.MarketSize,
            market.MarketTrend,
            Score100(market.MarketOpportunity),
            market.Summary,
            opportunity?.Explanation,
            Score100(competition?.Score),
            competition?.Explanation,
            [.. competitors.Select(c => new CompetitorDto(
                c.Name, c.Description, c.WebsiteUrl, c.Industry, c.PricingModel,
                c.Strengths, c.Weaknesses, Score100(c.SimilarityScore)))]));
    }

    private static async Task<IResult> HandleGetInnovation(Guid id,
        AnalysisDbContext db,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (!await OwnsAsync(db, id, currentUser, ct)) return Results.NotFound();

        var i = await db.InnovationAnalyses.AsNoTracking().FirstOrDefaultAsync(x => x.AnalysisId == id, ct);
        if (i is null) return Results.NotFound();

        var scores = await ScoresAsync(db, id, ct);
        var overall = scores.GetValueOrDefault(ScoreType.Innovation);

        return Results.Ok(new InnovationAnalysisDto(
            Score100(i.OriginalityScore)!.Value, i.OriginalityExplanation,
            Score100(i.TechnologicalInnovationScore)!.Value, i.TechnologicalInnovationExplanation,
            Score100(i.BusinessInnovationScore)!.Value, i.BusinessInnovationExplanation,
            Score100(i.GrowthPotentialScore)!.Value, i.GrowthPotentialExplanation,
            i.Summary,
            Score100(overall?.Score),
            overall?.Explanation));
    }

    private static Task<bool> OwnsAsync(AnalysisDbContext db, Guid id, ICurrentUser user, CancellationToken ct) =>
        db.Analyses.AnyAsync(a => a.Id == id && a.UserId == user.Id, ct);

    private static async Task<Dictionary<ScoreType, AnalysisScore>> ScoresAsync(AnalysisDbContext db, Guid id, CancellationToken ct) =>
        (await db.Scores.AsNoTracking().Where(s => s.AnalysisId == id).ToListAsync(ct))
            .GroupBy(s => s.ScoreType)
            .ToDictionary(g => g.Key, g => g.First());

    // The worker saves what the AI returned; the schema can't enforce the range
    private static int? Score100(decimal? score) => score is null ? null : Math.Clamp((int)score.Value, 1, 100);
    
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
    m.Key, m.Title, m.MiIcon,
    Status = modules.FirstOrDefault(r => r.Module == m.Key)?.Status ?? "Pending"
}).ToList();

    return Results.Ok(new
    {
        analysis.Id,
        Total = list.Count,
        Done = list.Count(m => m.Status is "Completed" or "Failed"),
        Modules = list
    });
    }
}
