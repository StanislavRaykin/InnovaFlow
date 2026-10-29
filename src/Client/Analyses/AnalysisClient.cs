using System.Net;
using System.Net.Http.Json;
using Contracts.AnalysisDtos;

namespace Client.Analyses;

public record AnalysisStartedResult(bool Success, Guid AnalysisId, string? Error = null);

public class AnalysisClient(HttpClient http, ILogger<AnalysisClient> logger)
{
    private readonly HttpClient http = http;
    private readonly ILogger<AnalysisClient> logger = logger;

    // queues the analysis; the worker picks it up and the client follows it by its id
    public async Task<AnalysisStartedResult> StartAnalysisAsync(Guid ideaId)
    {
        try
        {
            var response = await http.PostAsync($"api/analyses?ideaId={ideaId}",null);

            if (response.IsSuccessStatusCode)
            {
                var accepted = await response.Content.ReadFromJsonAsync<AnalysisAcceptedDto>();
                return new AnalysisStartedResult(true, accepted!.Id);
            }

            logger.LogWarning("Analysis for idea {IdeaId} wasn't started: {Status}", ideaId, response.StatusCode);
            return response.StatusCode switch
            {
                HttpStatusCode.Conflict => new AnalysisStartedResult(false, Guid.Empty, "Анализът вече е стартиран."),
                HttpStatusCode.NotFound => new AnalysisStartedResult(false, Guid.Empty, "Идеята не беше намерена."),
                _ => new AnalysisStartedResult(false, Guid.Empty, "Анализът не беше стартиран. Опитайте по-късно."),
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Analysis for idea {IdeaId} could not be started", ideaId);
            return new AnalysisStartedResult(false, Guid.Empty, "Анализът не беше стартиран. Опитайте по-късно.");
        }
    }
}
