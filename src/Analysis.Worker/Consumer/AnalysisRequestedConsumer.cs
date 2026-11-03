using Analysis.Worker.Modules;
using Contracts;
using Contracts.AnalysisDtos;
using InnovaFlow.Analysis.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
namespace Analysis.Worker.Consumer;

public class AnalysisRequestedConsumer(AnalysisDbContext db,
    IEnumerable<IAnalysisModule> modules,
    IPublishEndpoint bus,
    ILogger<AnalysisRequestedConsumer> logger) : IConsumer<AnalysisRequested>
{


    public async Task Consume(ConsumeContext<AnalysisRequested> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var analysis = await db.Analyses.FindAsync([msg.AnalysisId], ct);
        if (analysis == null)
        {
            logger.LogWarning("Analysis not found. Dropping message");
            return;
        }

        if (analysis.Status == AnalysisStatus.Running || analysis.Status == AnalysisStatus.Completed)
        {
            logger.LogInformation("Analysis {Id} already {Status} — skipping.", msg.AnalysisId, analysis.Status);
            return;
        }

        analysis.Status = AnalysisStatus.Running;
        analysis.TotalModules = modules.Count();
        analysis.CompletedModules = 0;
        await db.SaveChangesAsync(ct);

        await bus.Publish(new AnalysisStarted(msg.AnalysisId, msg.IdeaId), ct);

        var completed = 0;

        try
        {
            foreach (var stage in modules.GroupBy(m => m.Stage).OrderBy(g => g.Key))
            {
                // Within a stage modules are independent, so run them together.
                // With 10+ analyzers this is the difference between 30s and 5 minutes.
                await Task.WhenAll(stage.Select(async module =>
                {
                    logger.LogInformation("Running {Module} for analysis {Id}", module.Name, msg.AnalysisId);
                    await module.RunAsync(msg, ct);

                    var done = Interlocked.Increment(ref completed);
                    await bus.Publish(new AnalysisModuleCompleted(
                        msg.AnalysisId, msg.IdeaId, module.Name, done, modules.Count()), ct);
                }));

                // Persist progress after each stage so polling clients see movement.
                analysis.CompletedModules = completed;
                await db.SaveChangesAsync(ct);
            }

            analysis.OverallScore = await ComputeOverallAsync(msg.AnalysisId, ct);
            analysis.Status = AnalysisStatus.Completed;
            analysis.CompletedAt = DateTime.UtcNow;
            analysis.CompletedModules = completed;
            await db.SaveChangesAsync(ct);

            await bus.Publish(new AnalysisCompleted(
                msg.AnalysisId, msg.IdeaId, analysis.OverallScore ?? 0), ct);

            logger.LogInformation("Analysis {Id} completed with score {Score}",
                msg.AnalysisId, analysis.OverallScore);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Analysis {Id} failed after {Done}/{Total} modules",
                msg.AnalysisId, completed, modules.Count());

            // A failed module leaves its rejected entities tracked on this shared context,
            // and saving them again would throw here and strand the analysis as Running.
            db.ChangeTracker.Clear();
            db.Analyses.Attach(analysis);

            analysis.Status = AnalysisStatus.Failed;
            analysis.ErrorMessage = ex.Message;
            analysis.CompletedAt = DateTime.UtcNow;
            analysis.CompletedModules = completed;
            await db.SaveChangesAsync(CancellationToken.None);

            await bus.Publish(new AnalysisFailed(msg.AnalysisId, msg.IdeaId, ex.Message),
                CancellationToken.None);

            
        }
    }

    private async Task<decimal> ComputeOverallAsync(Guid analysisId, CancellationToken ct)
    {
        var scores = await db.Scores
            .Where(s => s.AnalysisId == analysisId && s.ScoreType != ScoreType.Innovation)
            .ToDictionaryAsync(s => s.ScoreType, s => s.Score);

        if (scores.Count == 0) return 0;

        var weights = new Dictionary<ScoreType, decimal>
        {
            [ScoreType.Market] = 0.25m,
            [ScoreType.Innovation]        = 0.20m,
            [ScoreType.Feasibility]       = 0.20m,
            [ScoreType.Scalability]       = 0.15m,
            [ScoreType.Competition]       = 0.10m,
            [ScoreType.Risk]              = 0.10m
        };

        var inverted = new[] { ScoreType.Competition, ScoreType.Risk };

        decimal total = 0;
        decimal weightUsed = 0;

        foreach (var (type, raw) in scores)
        {
            if (!weights.TryGetValue(type, out var w)) continue;

            var value = inverted.Contains(type) ? 100 - raw : raw;
            total += value * w;
            weightUsed += w;
        }

        // Re-normalise: with only 2 of 6 dimensions scored in week 1, a raw weighted sum
        // would cap at ~35 and the dashboard would look broken.
        return weightUsed == 0 ? 0 : Math.Round(total / weightUsed, 2);
    }

}
