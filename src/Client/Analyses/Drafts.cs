namespace Client.Analyses;

// Client copies of the AI's JSON for each module (camelCase on the wire). Everything is nullable:
// a live snapshot is cut off mid-stream, so any field can still be missing. The saved result
// from the API has the same shape and is read into the same type.

public sealed class AudienceDraft
{
    public int? OverallScore { get; set; }
    public string? OverallExplanation { get; set; }
    public SegmentDraft? PrimarySegment { get; set; }
    public List<SegmentDraft>? SecondarySegments { get; set; }
    public List<PersonaDraft>? Personas { get; set; }
    public string? Description { get; set; }
}

public sealed class SegmentDraft
{
    public string? Name { get; set; }
    public bool? IsHypothesis { get; set; }
    public string? Need { get; set; }
    public string? Motivation { get; set; }
    public string? UsageContext { get; set; }
    public string? Objections { get; set; }
    public string? PriceSensitivity { get; set; }
    public string? ReachDifficulty { get; set; }
    public int? AttractivenessScore { get; set; }
    public string? AttractivenessExplanation { get; set; }
}

public sealed class PersonaDraft
{
    public string? Name { get; set; }
    public string? AgeRange { get; set; }
    public string? Occupation { get; set; }
    public string? Description { get; set; }
    public List<string>? Goals { get; set; }
    public List<string>? PainPoints { get; set; }
    public List<string>? Behaviors { get; set; }
}

/// <summary>Mirrors MarketAnalysisResult in Analysis.Worker.</summary>
public sealed class MarketDraft
{
    public string? MarketSize { get; set; }
    public string? MarketTrend { get; set; }
    public int? MarketOpportunity { get; set; }
    public string? Summary { get; set; }
    public string? MarketOpportunityExplanation { get; set; }
    public int? CompetitionIntensity { get; set; }
    public string? CompetitionExplanation { get; set; }
    public List<CompetitorDraft>? Competitors { get; set; }
}

public sealed class CompetitorDraft
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Industry { get; set; }
    public string? PricingModel { get; set; }
    public List<string>? Strengths { get; set; }
    public List<string>? Weaknesses { get; set; }
    public int? SimilarityScore { get; set; }
}

/// <summary>Mirrors InnovationAnalysisResult in Analysis.Worker.</summary>
public sealed class InnovationDraft
{
    public int? OriginalityScore { get; set; }
    public string? OriginalityExplanation { get; set; }
    public int? TechnologicalInnovationScore { get; set; }
    public string? TechnologicalInnovationExplanation { get; set; }
    public int? BusinessInnovationScore { get; set; }
    public string? BusinessInnovationExplanation { get; set; }
    public int? GrowthPotentialScore { get; set; }
    public string? GrowthPotentialExplanation { get; set; }
    public string? Summary { get; set; }
    public int? OverallScore { get; set; }
    public string? OverallExplanation { get; set; }
}
