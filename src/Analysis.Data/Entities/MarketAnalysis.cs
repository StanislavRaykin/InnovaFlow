namespace InnovaFlow.Analysis.Data;

// ---------------------------------------------------------------------------
// Single-row node results
//
// Each has a unique AnalysisId: one result per node per run, which is what
// stops a rerun from duplicating them.
// ---------------------------------------------------------------------------

public class MarketAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string? MarketSize { get; set; }
    public string? MarketTrend { get; set; }
    public decimal? MarketOpportunity { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
