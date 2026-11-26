using Anthropic.Helpers;
using Contracts.Analysis;

[SchemaClass("Feasibility analysis of a user's idea: can it realistically be built with the information provided. Do not invent budgets, teams, partners or statistics. Everything in bulgarian!")]
public class FeasibilityAnalysisResult
{
    [SchemaProperty("Short overview shown under the idea name, 2-3 sentences (max ~300 characters). Whether the idea can realistically work and its main technical and resource requirements. No scores or lists.")]
    public string Summary { get; set; } = "";

    // ── Scores ──────────────────────────────────────────────────────

    [SchemaProperty("Technical feasibility, integer 1-100 (100 is straightforward with off-the-shelf technology, 1 needs unproven or unavailable technology).", Minimum = 1, Maximum = 100)]
    public int TechnicalScore { get; set; }

    [SchemaProperty("Resource requirements (capital, people, partners, materials), integer 1-100 (100 is extremely resource-heavy, 1 can be started by one person with minimal budget).", Minimum = 1, Maximum = 100)]
    public int ResourceScore { get; set; }

    [SchemaProperty("Implementation complexity (components, integrations, dependencies), integer 1-100 (100 is extremely complex, 1 is a single simple component).", Minimum = 1, Maximum = 100)]
    public int ComplexityScore { get; set; }

    [SchemaProperty("Market scalability, integer 1-100 (100 is easily replicable at large scale with low marginal cost, 1 is inherently local or niche).", Minimum = 1, Maximum = 100)]
    public int ScalabilityScore { get; set; }

    // ── Lists ───────────────────────────────────────────────────────

    [SchemaProperty("2-6 key technologies the idea actually needs, most important first. Do not pad the list.")]
    public List<TechnologyResult> Technologies { get; set; } = [];

    [SchemaProperty("2-5 main resource needs (capital, specialists, manufacturing or distribution partners, materials, infrastructure), most demanding first.")]
    public List<ResourceResult> Resources { get; set; } = [];

    [SchemaProperty("2-4 main obstacles to implementing the idea, most severe first. Concrete to this idea, not generic risks.")]
    public List<ChallengeResult> Challenges { get; set; } = [];
}

public class TechnologyResult
{
    [SchemaProperty("Short technology name in Bulgarian, max ~40 characters (e.g. 'BLE 5.0 Комуникация', 'Edge AI обработка').")]
    public string Name { get; set; } = "";

    [SchemaProperty("1-2 sentences in Bulgarian (max ~160 characters): how available/mature the technology is and what work is needed to use it for this idea.")]
    public string Description { get; set; } = "";

    [SchemaProperty("Available = mature and usable largely as-is. NeedsWork = exists but needs significant R&D, adaptation or integration for this idea.")]
    public TechnologyStatus Status { get; set; }
}

public class ResourceResult
{
    [SchemaProperty("Short resource label in Bulgarian, max ~30 characters (e.g. 'Капитал за R&D', 'Специалисти Хардуер', 'Партньори Производство').")]
    public string Name { get; set; } = "";

    [SchemaProperty("How important and hard to secure. Low = easy, not essential; Medium = needed, obtainable with normal effort; High = essential and costly or hard to obtain; Critical = the idea cannot proceed without it and it is scarce.")]
    public SeverityLevel Level { get; set; }

    [SchemaProperty("Size of the need, integer 1-100 (100 is the largest need). Must agree with Level: Low 1-30, Medium 31-60, High 61-85, Critical 86-100.", Minimum = 1, Maximum = 100)]
    public int Intensity { get; set; }
}

public class ChallengeResult
{
    [SchemaProperty("Short challenge name in Bulgarian, max ~30 characters (e.g. 'Живот на батерията', 'Издръжливост при пране').")]
    public string Title { get; set; } = "";

    [SchemaProperty("1-2 sentences in Bulgarian (max ~140 characters): what the problem is and what it would take to overcome it.")]
    public string Description { get; set; } = "";

    [SchemaProperty("Impact on feasibility. Low = minor inconvenience; Medium = manageable; High = could significantly delay or raise the cost; Critical = could make the idea unworkable if not solved.")]
    public SeverityLevel Severity { get; set; }

    [SchemaProperty("Type of challenge: Technical = software/algorithms/integration; Hardware = physical components; Energy = power/battery; Durability = wear, washing, environment; Operational = production, logistics, staffing; Financial = cost and funding; Legal = regulation, certification, personal data; Market = adoption and competition; Other = anything else.")]
    public ChallengeCategory Category { get; set; }
}

