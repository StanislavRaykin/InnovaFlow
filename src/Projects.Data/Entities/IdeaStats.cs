namespace InnovaFlow.Projects.Data;

/// <summary>
/// Denormalised counters for the dashboard, updated from analysis events
/// consumed off RabbitMQ. Avoids a cross-service query on every page load.
/// </summary>
public class IdeaStats
{
    public Guid IdeaId { get; set; }
    public int TotalAnalyses { get; set; }
    public int CompletedAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    public decimal? LatestOverallScore { get; set; }
    public DateTimeOffset? LastAnalysedAt { get; set; }
}
