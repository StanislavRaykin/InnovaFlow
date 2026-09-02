namespace InnovaFlow.Analysis.Data;

public class Opportunity
{
    public Guid Id { get; set; }

    public Guid OpportunityAnalysisId { get; set; }
    public OpportunityAnalysis OpportunityAnalysis { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? PotentialImpact { get; set; }
    public string? Difficulty { get; set; }
    public string? Recommendation { get; set; }
}
