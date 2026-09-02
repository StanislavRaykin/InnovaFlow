namespace InnovaFlow.Projects.Data;

public class Strategy
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Free text - "Premium", "Mass Market", "Technology First".</summary>
    public string? StrategyType { get; set; }

    /// <summary>
    /// At most one selected strategy per idea, enforced by a partial unique
    /// index rather than by remembering to clear the previous one.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// The analysis that proposed this strategy. Lives in the analysis schema,
    /// so no FK.
    /// </summary>
    public Guid? ProposedByAnalysisId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<StrategyMetric> Metrics { get; set; } = [];
}
