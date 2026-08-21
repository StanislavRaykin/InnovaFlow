namespace InnovaFlow.Projects.Data;

/// <summary>
/// Read model maintained by consuming JobFinished. Exists because Projects
/// cannot query the analysis schema - the event carries the counts instead.
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
