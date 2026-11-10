namespace Analysis.Worker.Modules.Innovation;
using Anthropic.Helpers;

[SchemaClass("Innovation and originality assessment of a user's idea. All in bulgarian")]
public sealed class InnovationAnalysisResult
{
    // ── InnovationAnalyses ──────────────────────────────────────────

    [SchemaProperty("Originality / novelty of the product or service compared to the alternatives, integer 1-100.", Minimum = 1, Maximum = 100)]
    public int OriginalityScore { get; set; }

    [SchemaProperty("Why OriginalityScore has this value and how confident you are (low/medium/high), 1-3 sentences in Bulgarian.")]
    public string OriginalityExplanation { get; set; } = "";

    [SchemaProperty("Technological innovation, integer 1-100.", Minimum = 1, Maximum = 100)]
    public int TechnologicalInnovationScore { get; set; }

    [SchemaProperty("Why TechnologicalInnovationScore has this value and how confident you are (low/medium/high), 1-3 sentences.")]
    public string TechnologicalInnovationExplanation { get; set; } = "";

    [SchemaProperty("Business model innovation, integer 1-100.", Minimum = 1, Maximum = 100)]
    public int BusinessInnovationScore { get; set; }

    [SchemaProperty("Why BusinessInnovationScore has this value, 1-3 sentences.")]
    public string BusinessInnovationExplanation { get; set; } = "";

    [SchemaProperty("Growth potential, integer 1-100.", Minimum = 1, Maximum = 100)]
    public int GrowthPotentialScore { get; set; }

    [SchemaProperty("Why GrowthPotentialScore has this value, 1-3 sentences.")]
    public string GrowthPotentialExplanation { get; set; } = "";

    [SchemaProperty("""
        Summary, short paragraphs separated by blank lines. Must contain, in this order:
        1) if no competitor analysis was provided: that originality cannot be fully confirmed;
        2) up to three realistic ways to differentiate, fitting the stage, resources and constraints if given;
        3) which features are only ideas and which are already implemented;
        4) the main unknowns;
        5) exactly three small experiments to test the innovation assumptions.
        """)]
    public string Summary { get; set; } = "";

    // ── AnalysisScores (ScoreType.Innovation) ───────────────────────

    [SchemaProperty("Overall innovation score, integer 1-100.", Minimum = 1, Maximum = 100)]
    public int OverallScore { get; set; }

    [SchemaProperty("Overall assessment: why OverallScore has this value, 2-4 sentences.")]
    public string OverallExplanation { get; set; } = "";
}
