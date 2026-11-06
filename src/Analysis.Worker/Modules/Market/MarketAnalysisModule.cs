using Analysis.Worker.AI;
using Anthropic.Exceptions;
using Contracts;
using Contracts.AnalysisDtos;
using InnovaFlow.Analysis.Data;
using MassTransit.Internals.GraphValidation;
using Microsoft.EntityFrameworkCore;

namespace Analysis.Worker.Modules.Market;

public class MarketAnalysisModule(AnalysisDbContext db, IAIClientFactory aiFactory, ILogger<MarketAnalysisModule> logger) : IAnalysisModule
{

    public int Stage => 1;

    public string Name => "MarketAnalysis";


    public async Task RunAsync(AnalysisRequested req, CancellationToken ct)
    {
        AIRequest researchRequest = new AIRequest
        {
            AnalysisId = req.AnalysisId,
            AgentType = NodeType.Market,
            Status = Enum.GetName(AnalysisStatus.Running),
            StartedAt = DateTimeOffset.UtcNow
        };
        AIRequest scoreRequest = researchRequest;


        db.Requests.Add(researchRequest);
        db.Requests.Add(scoreRequest);
        
        await db.SaveChangesAsync(ct);

        AiResponse researchResult = null;
        AiResult<MarketAnalysisResult> scoreResult = null;
        
        var researcher = aiFactory.GetAiClient("Kimi");
        try
        {
            string user = IdeaPromptBuilder.Build(req, MarketPrompt.ResearchTask);
            researchResult = await researcher.CompleteTextAsync(MarketPrompt.ResearchSystem, user, ct);

        }
        catch (Exception ex)
        {
            await MarkFailedAsync(researchRequest, ex, ct);

        }

        var scorer = aiFactory.GetAiClient("Claude");
        try
        {
            string sources = string.Join("\n", researchResult?.Sources?.Select(s => $"[{s.Number}] {s.Title} ({s.Url})") ?? Array.Empty<string>());
            string user = IdeaPromptBuilder.Build(req, MarketPrompt.ScoreTask) + 
            $"<research>\n{(researchResult is null ? "No research available" : researchResult.Text)}\n</research>"
            + $"<sources>\n{sources}\n</sources>";
            scoreResult = await scorer.CompleteAsync<MarketAnalysisResult>(MarketPrompt.ScoreSystem, user, ct);
            // End result processing
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(scoreRequest, ex, ct);
            throw;
        }

        var marketAnalysis = scoreResult.Value;
        var execStrategy = db.Database.CreateExecutionStrategy();
        await execStrategy.ExecuteAsync(async () =>
        {
            using var t = db.Database.BeginTransaction();

            DeletePreviousResultsAsync(req.AnalysisId, ct).Wait(ct);

            db.MarketAnalyses.Add(new MarketAnalysis
            {
                AnalysisId = req.AnalysisId,
                MarketSize = marketAnalysis.MarketSize,
                MarketTrend = marketAnalysis.MarketTrend,
                MarketOpportunity = marketAnalysis.MarketOpportunity,
                Summary = marketAnalysis.Summary,
                CreatedAt = DateTimeOffset.UtcNow
            });

            db.Competitors.AddRange(marketAnalysis.Competitors.Select(c => new Competitor
            {
                Id = Guid.CreateVersion7(),
                AnalysisId = req.AnalysisId,
                Source = Name,
                Name = c.Name,
                Description = c.Description,
                WebsiteUrl = c.WebsiteUrl,
                Industry = c.Industry,
                PricingModel = c.PricingModel,
                Strengths = c.Strengths,
                Weaknesses = c.Weaknesses,
                SimilarityScore = c.SimilarityScore
            }));

            db.Scores.AddRange(
                NewScore(req.AnalysisId, ScoreType.Market, marketAnalysis.MarketOpportunity, marketAnalysis.MarketOpportunityExplanation),
                NewScore(req.AnalysisId, ScoreType.Competition, marketAnalysis.CompetitionIntensity, marketAnalysis.CompetitionExplanation));

            await MarkSucceededAsync(scoreRequest, researchResult, ct);
            await MarkSucceededAsync(researchRequest, researchResult, ct);

            await db.SaveChangesAsync(ct);
            await t.CommitAsync(ct);
        });

        logger.LogInformation($"Market analysis completed. {marketAnalysis.Competitors.Count} competitors found, market opportunity: {marketAnalysis.MarketOpportunity}, competition intensity: {marketAnalysis.CompetitionIntensity}");

    }

    // in case of a rerun
    private async Task DeletePreviousResultsAsync(Guid analysisId, CancellationToken ct)
    {
        await db.MarketAnalyses.Where(m => m.AnalysisId == analysisId).ExecuteDeleteAsync(ct);
        await db.Competitors.Where(c => c.AnalysisId == analysisId && c.Source == Name).ExecuteDeleteAsync(ct);
        await db.Scores
            .Where(x => x.AnalysisId == analysisId &&
                        (x.ScoreType == ScoreType.Market || x.ScoreType == ScoreType.Competition))
            .ExecuteDeleteAsync(ct);
    }


    private async Task MarkFailedAsync(AIRequest aiRequest, Exception ex, CancellationToken ct)
    {
        aiRequest.Status = AnalysisStatus.Failed.ToString();
        aiRequest.ErrorMessage = ex is OperationCanceledException ? "canceled" : ex.Message;
        aiRequest.CompletedAt = DateTimeOffset.UtcNow;

        if (ex is AiClientException aiEx)
        {
            aiRequest.Model = aiEx.Response.Model;
            aiRequest.PromptTokens = (int)aiEx.Response.InputTokens;
            aiRequest.CompletionTokens = (int)aiEx.Response.OutputTokens;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task MarkSucceededAsync(AIRequest aiRequest, AiResponse result, CancellationToken ct)
    {
        aiRequest.Status = AnalysisStatus.Completed.ToString();
        aiRequest.CompletedAt = DateTimeOffset.UtcNow;
        aiRequest.Model = result.Model;
        aiRequest.PromptTokens = (int)result.InputTokens;
        aiRequest.CompletionTokens = (int)result.OutputTokens;

        await db.SaveChangesAsync(ct);
    }

    private AnalysisScore NewScore(Guid analysisId, ScoreType type, int score, string explanation) => new()
    {
        AnalysisId = analysisId,
        ScoreType = type,
        Score = score,
        Explanation = explanation
    };

}
