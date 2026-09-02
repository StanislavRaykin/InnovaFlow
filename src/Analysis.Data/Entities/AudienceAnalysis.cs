namespace InnovaFlow.Analysis.Data;

public class AudienceAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string? PrimaryAudience { get; set; }
    public string? SecondaryAudience { get; set; }
    public string? Description { get; set; }

    public List<Persona> Personas { get; set; } = [];
}
