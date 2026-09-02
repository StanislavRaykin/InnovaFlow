namespace InnovaFlow.Analysis.Data;

public class Analysis
{
    public Guid Id { get; set; }

    /// <summary>Owned by the Projects service. No FK - different schema.</summary>
    public Guid IdeaId { get; set; }

    /// <summary>
    /// The immutable snapshot this run analysed. Also lives in the projects
    /// schema, so also no FK. Without it, comparing analysis #1 against #3 is
    /// comparing results against text that may since have changed.
    /// </summary>
    public Guid IdeaVersionId { get; set; }

    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public decimal? OverallScore { get; set; }
    public string? Summary { get; set; }
    public string? ErrorMessage { get; set; }

    public List<AnalysisScore> Scores { get; set; } = [];
    public List<Recommendation> Recommendations { get; set; } = [];
    public List<Source> Sources { get; set; } = [];
    public List<AIRequest> Requests { get; set; } = [];
}
