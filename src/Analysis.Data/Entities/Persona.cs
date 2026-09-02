namespace InnovaFlow.Analysis.Data;

public class Persona
{
    public Guid Id { get; set; }

    public Guid AudienceAnalysisId { get; set; }
    public AudienceAnalysis AudienceAnalysis { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? AgeRange { get; set; }
    public string? Occupation { get; set; }
    public string? Description { get; set; }

    public List<string> Goals { get; set; } = [];
    public List<string> PainPoints { get; set; } = [];
    public List<string> Behaviors { get; set; } = [];
}
