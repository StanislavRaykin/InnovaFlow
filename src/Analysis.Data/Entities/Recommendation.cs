namespace InnovaFlow.Analysis.Data;

public class Recommendation
{
    public Guid Id { get; set; }

    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Priority Priority { get; set; }
    public string? Category { get; set; }
}
