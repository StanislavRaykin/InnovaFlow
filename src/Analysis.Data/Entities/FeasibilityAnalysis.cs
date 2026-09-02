namespace InnovaFlow.Analysis.Data;

public class FeasibilityAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public decimal? TechnicalScore { get; set; }
    public decimal? ResourceScore { get; set; }
    public decimal? ComplexityScore { get; set; }
    public string? Summary { get; set; }
}
