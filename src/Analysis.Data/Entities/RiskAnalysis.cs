namespace InnovaFlow.Analysis.Data;

public class RiskAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public decimal? RiskScore { get; set; }
    public string? Summary { get; set; }

    public List<Risk> Risks { get; set; } = [];
}
