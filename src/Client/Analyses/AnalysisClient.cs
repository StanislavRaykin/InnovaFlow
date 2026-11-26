using System.Net;
using System.Net.Http.Json;
using Contracts.AnalysisDtos;
using Client.Views;

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
                HttpStatusCode.NotFound => new AnalysisStartedResult(false, Guid.Empty, "Идеята не e намерена."),
                _ => new AnalysisStartedResult(false, Guid.Empty, "Анализът не беше стартиран. Опитайте по-късно."),
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Analysis for idea {IdeaId} could not be started", ideaId);
            return new AnalysisStartedResult(false, Guid.Empty, "Анализът не беше стартиран. Опитайте по-късно.");
        }
    }

    /// <summary>All the user's analyses, newest first. Throws HttpRequestException if the API can't be reached.</summary>
    public async Task<List<AnalysisSummaryDto>> GetAnalysesAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<AnalysisSummaryDto>>("api/analyses", ct) ?? [];

    public Task<AnalysisStatusView?> GetStatusAsync(Guid id, CancellationToken ct = default) =>
        GetOrNullAsync<AnalysisStatusView>($"api/analyses/{id}", ct);

    // Saved module results, in the same shape as the live drafts; null until the module has completed.

    public Task<AudienceDraft?> GetAudienceAsync(Guid id, CancellationToken ct = default) =>
        GetOrNullAsync<AudienceDraft>($"api/analyses/{id}/audience", ct);

    public Task<MarketDraft?> GetMarketAsync(Guid id, CancellationToken ct = default) =>
        GetOrNullAsync<MarketDraft>($"api/analyses/{id}/market", ct);

    public Task<InnovationDraft?> GetInnovationAsync(Guid id, CancellationToken ct = default) =>
        GetOrNullAsync<InnovationDraft>($"api/analyses/{id}/innovation", ct);

    public Task<FeasibilityDraft?> GetFeasibilityAsync(Guid id, CancellationToken ct = default) =>
        GetOrNullAsync<FeasibilityDraft>($"api/analyses/{id}/feasibility", ct);

    public Task<RiskDraft?> GetRisksAsync(Guid id, CancellationToken ct = default) =>
        GetOrNullAsync<RiskDraft>($"api/analyses/{id}/risks", ct);

    // 404 = missing or not yours → null; any other error throws
    private async Task<T?> GetOrNullAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var res = await http.GetAsync(url, ct);
        if (res.StatusCode == HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync<T>(ct);
    }
}
