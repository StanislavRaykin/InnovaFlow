namespace InnovaFlow.Analysis.Data;

public class BusinessAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string? BusinessModel { get; set; }
    public string? RevenueModel { get; set; }
    public string? PricingStrategy { get; set; }
    public string? CostAnalysis { get; set; }
    public decimal? RevenuePotential { get; set; }
    public string? Summary { get; set; }
}
