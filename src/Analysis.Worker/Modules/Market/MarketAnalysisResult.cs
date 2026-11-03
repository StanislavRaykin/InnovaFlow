namespace Analysis.Worker.Modules.Market;

using Anthropic.Helpers;


[SchemaClass("Market and competitive analysis of a user's idea. Everything in bulgarian!")]
public class MarketAnalysisResult
{

    [SchemaProperty("Qualitative description of the market size in, 1-3 sentences. No invented numbers or statistics.")]
    public string MarketSize { get; set; } = "";

    [SchemaProperty("Relevant market trends, 2-5 short points separated by new lines.")]
    public string MarketTrend { get; set; } = "";

    [SchemaProperty("Market potential, integer 1-100 (100 is really high potential).", Minimum = 1, Maximum = 100)]
    public int MarketOpportunity { get; set; }

    [SchemaProperty("""
        Summary in Bulgarian, short paragraphs separated by blank lines. Must contain, in this order:
        1) whether this is a preliminary assessment;
        2) possible niches and the niche opportunity, with rationale;
        3) differentiation potential, with rationale;
        4) what is known, what is assumed, which important data is missing and how to collect it;
        5) the most promising market segment hypothesis;
        6) exactly three concrete checks to do before investing.
        """)]
    public string Summary { get; set; } = "";

    // ── AnalysisScores ──────────────────────────────────────────────

    [SchemaProperty("Why MarketOpportunity has this value and how confident you are (low/medium/high), 1-3 sentences.")]
    public string MarketOpportunityExplanation { get; set; } = "";

    [SchemaProperty("Competition intensity, integer 1-100 (100 is extremely intense).", Minimum = 1, Maximum = 100)]
    public int CompetitionIntensity { get; set; }

    [SchemaProperty("Why CompetitionIntensity has this value and how confident you are (low/medium/high), 1-3 sentences.")]
    public string CompetitionExplanation { get; set; } = "";

    // ── Competitors ─────────────────────────────────────────────────

    [SchemaProperty("2-6 direct and indirect competitors or competitor categories.")]
    public List<CompetitorResult> Competitors { get; set; } = [];
}

public class CompetitorResult
{
    [SchemaProperty("A real, well-known competitor ONLY if you are certain it exists. Otherwise a competitor category name, e.g. 'Платформи за доставка на храна'. Never invent company names.")]
    public string Name { get; set; } = "";

    [SchemaProperty("Start with 'Пряк конкурент.' or 'Косвен конкурент.', then what it offers, 1-2 sentences in Bulgarian.")]
    public string Description { get; set; } = "";

    [SchemaProperty("Official website ONLY for a real company you are certain about. Exclude for categories or if unsure.")]
    public string? WebsiteUrl { get; set; }

    [SchemaProperty("Industry of the competitor in Bulgarian.")]
    public string Industry { get; set; } = "";

    [SchemaProperty("Typical pricing model in Bulgarian (e.g. 'Абонамент', 'Комисионна'). Exclude if unknown.")]
    public string? PricingModel { get; set; }

    [SchemaProperty("1-5 strengths.")]
    public List<string> Strengths { get; set; } = [];

    [SchemaProperty("1-5 weaknesses.")]
    public List<string> Weaknesses { get; set; } = [];

    [SchemaProperty("How similar to the user's idea, integer 1-100.", Minimum = 1, Maximum = 100)]
    public int SimilarityScore { get; set; }
}
