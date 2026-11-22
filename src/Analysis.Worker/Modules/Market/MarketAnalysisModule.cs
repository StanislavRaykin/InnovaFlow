using System.Diagnostics;
using Analysis.Worker.AI;
using Analysis.Worker.Publishers;
using Anthropic.Helpers;
using Contracts;
using Contracts.AnalysisDtos;
using InnovaFlow.Analysis.Data;
using Microsoft.EntityFrameworkCore;

namespace Analysis.Worker.Modules.Market;

public class MarketAnalysisModule(AnalysisDbContext db, PartialPublisher partials, ProgressPublisher progress, ModuleRunTracker runs, IAIClientFactory aiFactory, ILogger<MarketAnalysisModule> logger) : IAnalysisModule
{

    public int Stage => 1;

    public string Name => "MarketAnalysis";


    public async Task RunAsync(AnalysisRequested req, CancellationToken ct)
    {
        await runs.StartAsync(req.AnalysisId, Name, ct);
        try
        {
            await RunCoreAsync(req, ct);
        }
        catch (Exception ex)
        {
            await runs.FailAsync(req.AnalysisId, Name, ex);
            throw;
        }
        await runs.CompleteAsync(req.AnalysisId, Name);
    }

    private async Task RunCoreAsync(AnalysisRequested req, CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        AIRequest researchRequest = new AIRequest
        {
            AnalysisId = req.AnalysisId,
            AgentType = NodeType.Market,
            Status = AnalysisStatus.Running.ToString(),
            StartedAt = DateTimeOffset.UtcNow
        };
        AIRequest scoreRequest = new AIRequest
        {
            AnalysisId = req.AnalysisId,
            AgentType = NodeType.Market,
            Status = AnalysisStatus.Running.ToString(),
            StartedAt = DateTimeOffset.UtcNow
        };


        db.Requests.Add(researchRequest);
        db.Requests.Add(scoreRequest);

        await db.SaveChangesAsync(ct);
        await progress.LogAsync(req, Name, "Инициализиране на модул 'Пазар'... OK", ct);

        AiResponse? researchResult = null;

        // ── Step 1: research (Kimi + web search). Optional: on failure the scoring runs without it ──
        var researcher = aiFactory.GetAiClient("Kimi");
        try
        {
            await progress.ReportAsync(req, Name, ProgressStage.ResearchStarted, ct: ct);
            await progress.LogAsync(req, Name, "Проучване на пазара в реално време...", ct);

            string user = IdeaPromptBuilder.Build(req, MarketPrompt.ResearchTask);
            researchResult = await StreamResearchAsync(req, researcher.StreamTextAsync(MarketPrompt.ResearchSystem, user, ct), ct);

            await SaveSourcesAsync(req.AnalysisId, researchResult.Sources, ct);
            await MarkSucceededAsync(researchRequest, researchResult, ct);
            await progress.ReportAsync(req, Name, ProgressStage.ResearchCompleted, ct: ct);
            await progress.LogAsync(req, Name, $"Проучването приключи. Намерени източници: {researchResult.Sources?.Count ?? 0}.", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Market research failed, scoring without it");
            await MarkFailedAsync(researchRequest, ex, ct);
            await progress.ReportAsync(req, Name, ProgressStage.ResearchCompleted,
                "Проучването в реално време не беше успешно. Оценката е предварителна.", ct);
            await progress.LogAsync(req, Name, "Проучването не беше успешно. Продължаване с предварителна оценка...", ct);
        }

        var scorer = aiFactory.GetAiClient("Claude");
        AiStreamResult scoreResult;
        try
        {
            await progress.ReportAsync(req, Name, ProgressStage.ScoringStarted, ct: ct);
            await progress.LogAsync(req, Name, "Оценяване на пазарния потенциал и конкуренцията...", ct);

            string sources = string.Join("\n", researchResult?.Sources?.Select(s => $"[{s.Number}] {s.Title} ({s.Url})") ?? Array.Empty<string>());

            string user = IdeaPromptBuilder.Build(req, MarketPrompt.ScoreTask) +
            $"<research>\n{(researchResult is null ? "No research available" : researchResult.Text)}\n</research>"
            + $"<sources>\n{sources}\n</sources>";

            var stream = scorer.StreamAsync<MarketAnalysisResult>(MarketPrompt.ScoreSystem, user, ct);
            scoreResult = await partials.PumpAsync(req, Name, stream, ct);
        }
        catch (Exception ex)
        {
            await progress.ReportAsync(req, Name, ProgressStage.Failed, "Анализът на пазара се провали", CancellationToken.None);
            await MarkFailedAsync(scoreRequest, ex, CancellationToken.None);
            throw;
        }

        var marketAnalysis = scoreResult.Info is AiStreamCompleted<MarketAnalysisResult> typed
            ? typed.Value
            : StructuredOutput.Parse<MarketAnalysisResult>(scoreResult.Json);
        await progress.LogAsync(req, Name, "Записване на резултатите от пазарния анализ...", ct);
        var execStrategy = db.Database.CreateExecutionStrategy();
        await execStrategy.ExecuteAsync(async () =>
        {
            await using var t = await db.Database.BeginTransactionAsync(ct);

            await DeletePreviousResultsAsync(req.AnalysisId, ct);

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


            await MarkSucceededAsync(scoreRequest, scoreResult.Info.Response, ct);

            await db.SaveChangesAsync(ct);
            await t.CommitAsync(ct);
        });

        await progress.ReportAsync(req, Name, ProgressStage.Completed, ct: ct);
        await progress.LogAsync(req, Name,
            $"Модул 'Пазар' приключи за {sw.Elapsed.TotalSeconds:0.0} с. Пазарен потенциал: {marketAnalysis.MarketOpportunity}/100, конкуренти: {marketAnalysis.Competitors.Count}.", ct);
        logger.LogInformation("Market analysis completed and took {Elapsed}. {MarketAnalysisCompetitorsCount} competitors found, market opportunity: {MarketAnalysisMarketOpportunity}, competition intensity: {MarketAnalysisCompetitionIntensity}",sw.Elapsed, marketAnalysis.Competitors.Count, marketAnalysis.MarketOpportunity, marketAnalysis.CompetitionIntensity);

    }

    /// <summary>Drains the research stream, reporting each web search as it starts.</summary>
    private async Task<AiResponse> StreamResearchAsync(AnalysisRequested req, IAsyncEnumerable<AiStreamEvent> stream, CancellationToken ct)
    {
        await foreach (var ev in stream.WithCancellation(ct))
        {
            switch (ev)
            {
                case AiWebSearch search:
                    await progress.ReportAsync(req, Name, ProgressStage.Searching, search.Query, ct);
                    break;
                case AiStreamCompleted done:
                    return done.Response;
            }
        }

        throw new AiClientException("Research stream ended without a final response");
    }

    private async Task SaveSourcesAsync(Guid analysisId, IReadOnlyList<AiSource>? sources, CancellationToken ct)
    {
        // incase of a rerun
        await db.Sources.Where(s => s.AnalysisId == analysisId).ExecuteDeleteAsync(ct);

        if (sources is not { Count: > 0 }) return;

        var now = DateTimeOffset.UtcNow;
        db.Sources.AddRange(sources.Select(s => new Source
        {
            AnalysisId = analysisId,
            SourceType = "text",
            Title = s.Title,
            Url = s.Url,
            RetrievedAt = now
        }));
        await db.SaveChangesAsync(ct);
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

        if (ex is AiClientException { Response: { } response })
        {
            aiRequest.Model = response.Model;
            aiRequest.PromptTokens = (int)response.InputTokens;
            aiRequest.CompletionTokens = (int)response.OutputTokens;
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
