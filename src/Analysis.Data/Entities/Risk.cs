namespace InnovaFlow.Analysis.Data;

public class Risk
{
    public Guid Id { get; set; }

    public Guid RiskAnalysisId { get; set; }
    public RiskAnalysis RiskAnalysis { get; set; } = null!;

    /// <summary>Market, financial, technical, legal, security, competitive, scaling.</summary>
    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public decimal? Probability { get; set; }
    public decimal? Impact { get; set; }
    public RiskSeverity Severity { get; set; }
    public string? Mitigation { get; set; }
}
