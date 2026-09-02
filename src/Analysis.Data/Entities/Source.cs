namespace InnovaFlow.Analysis.Data;

/// <summary>
/// Where a claim came from. Requirement 4 says the system must explain how a
/// score was formed; unsourced output is the fastest way to lose a jury.
/// </summary>
public class Source
{
    public Guid Id { get; set; }

    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Url { get; set; }
    public string? SourceType { get; set; }
    public DateTimeOffset RetrievedAt { get; set; }
    public decimal? RelevanceScore { get; set; }
}
