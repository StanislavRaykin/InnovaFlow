using Analysis.Worker.AI;
using Anthropic.Exceptions;
using Contracts;
using Contracts.AnalysisDtos;
using InnovaFlow.Analysis.Data;
using MassTransit.Internals.GraphValidation;
using Microsoft.EntityFrameworkCore;
using static Analysis.Worker.Modules.AnalyzerPromptRules;

namespace Analysis.Worker.Modules.Market;

public class MarketAnalysisModule(AnalysisDbContext db, IAIClientFactory aiFactory, ILogger<MarketAnalysisModule> logger) : IAnalysisModule
{

    public int Stage => 1;

    public string Name => "MarketAnalysis";

    private const string SystemPrompt = """
        Използвай входните полета на бизнес идеята, подадени от потребителя, за да направиш анализ на пазара и конкурентната среда. 
        Определи вероятните категории преки и косвени конкуренти, съществуващи алтернативи, релевантни пазарни тенденции и възможни ниши. 
        Не приемай, че идеята е уникална без доказателства и не измисляй конкретни конкуренти или пазарни статистики.
         Ако имаш достъп до надеждно актуално търсене, използвай го и разграничавай проверените източници от собствените си изводи; ако нямаш, изрично посочи, че това е предварителна оценка. 
         Оцени от 1 до 100 пазарния потенциал, интензивността на конкуренцията, възможността за намиране на ниша и потенциала за диференциация. 
         За всяка оценка обясни основанието и увереността. Посочи какво е известно, какво е допускане, кои важни данни липсват и как те могат да бъдат събрани. 
         Завърши с най-обещаващата хипотеза за пазарен сегмент и три конкретни проверки, които трябва да се направят преди инвестиция.
 
        Формат на отговора:
        - Пазарният потенциал и интензивността на конкуренцията се връщат като отделни оценки с обяснение.
        - Възможността за ниша, потенциалът за диференциация, известното, допусканията, липсващите данни, хипотезата за сегмент и трите проверки се включват в обобщението (summary), в реда, описан в схемата.
        - Съществуващите алтернативи се връщат като конкуренти (косвени), а не като отделен списък.
        """
    + AnalyzerPromptRules.Common
    + AnalyzerPromptRules.NoLiveSearch;

    public const string Task = "Направи анализ на пазара и конкурентната среда за тази идея.";


    public async Task RunAsync(AnalysisRequested req, CancellationToken ct)
    {
        AIRequest aiRequest = new AIRequest
        {
            AnalysisId = req.AnalysisId,
            AgentType = NodeType.Market,
            Status = Enum.GetName(AnalysisStatus.Running),
            StartedAt = DateTimeOffset.UtcNow
        };


        db.Requests.Add(aiRequest);
        await db.SaveChangesAsync(ct);

        AiResult<MarketAnalysisResult> result = null;
        var ai = aiFactory.GetAiClient("Claude");
        try
        {
            string user = IdeaPromptBuilder.Build(req, Task);
            result = await ai.CompleteAsync<MarketAnalysisResult>(SystemPrompt, user, ct);

        }
        catch (Exception ex)
        {
            await MarkFailedAsync(aiRequest, ex, ct);
            throw;
        }

        var marketAnalysis = result.Value;
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

            aiRequest.Status = AnalysisStatus.Completed.ToString();
            aiRequest.CompletedAt = DateTime.UtcNow;
            aiRequest.Model = result.Raw.Model;
            aiRequest.PromptTokens = (int)result.Raw.InputTokens;
            aiRequest.CompletionTokens = (int)result.Raw.OutputTokens;

            await db.SaveChangesAsync(ct);
            await t.CommitAsync(ct);
        });

        logger.LogInformation($"Market analysis completed. {marketAnalysis.Competitors.Count} competitors found, market opportunity: {marketAnalysis.MarketOpportunity}, competition intensity: {marketAnalysis.CompetitionIntensity}");


    }

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

    private AnalysisScore NewScore(Guid analysisId, ScoreType type, int score, string explanation) => new()
    {
        AnalysisId = analysisId,
        ScoreType = type,
        Score = score,
        Explanation = explanation
    };

}
