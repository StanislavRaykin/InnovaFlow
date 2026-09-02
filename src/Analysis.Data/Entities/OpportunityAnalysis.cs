namespace InnovaFlow.Analysis.Data;

public class OpportunityAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public decimal? OpportunityScore { get; set; }
    public string? Summary { get; set; }

    public List<Opportunity> Opportunities { get; set; } = [];
}
