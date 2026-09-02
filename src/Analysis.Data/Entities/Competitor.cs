namespace InnovaFlow.Analysis.Data;

// ---------------------------------------------------------------------------
// Multi-row node results
//
// No unique constraint is possible here, so the Worker must delete by parent
// id before inserting when a node reruns.
// ---------------------------------------------------------------------------

public class Competitor
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Industry { get; set; }
    public string? PricingModel { get; set; }

    // Leaf-level string lists. jsonb here, not child tables - nothing joins
    // or filters on an individual strength.
    public List<string> Strengths { get; set; } = [];
    public List<string> Weaknesses { get; set; } = [];

    public decimal? SimilarityScore { get; set; }
}
