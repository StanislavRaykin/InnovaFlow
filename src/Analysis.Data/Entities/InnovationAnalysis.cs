namespace InnovaFlow.Analysis.Data;

public class InnovationAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }

    public Analysis Analysis { get; set; } = null!;
 
    public decimal OriginalityScore { get; set; }
    public string OriginalityExplanation { get; set; } = "";
 
    public decimal TechnologicalInnovationScore { get; set; }
    public string TechnologicalInnovationExplanation { get; set; } = "";
 
    public decimal BusinessInnovationScore { get; set; }
    public string BusinessInnovationExplanation { get; set; } = "";
 
    public decimal GrowthPotentialScore { get; set; }
    public string GrowthPotentialExplanation { get; set; } = "";
 
    public string Summary { get; set; } = "";
 
    public DateTime CreatedAt { get; set; }
}
