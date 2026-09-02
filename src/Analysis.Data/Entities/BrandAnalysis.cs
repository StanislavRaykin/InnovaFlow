namespace InnovaFlow.Analysis.Data;

/// <summary>Only produced for brand-type ideas. Absent rows are normal.</summary>
public class BrandAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string? Positioning { get; set; }
    public string? CommunicationStyle { get; set; }
    public string? VisualIdentity { get; set; }

    public List<string> CoreValues { get; set; } = [];
    public List<string> NameSuggestions { get; set; } = [];
    public List<string> Slogans { get; set; } = [];

    public string? Summary { get; set; }
}
