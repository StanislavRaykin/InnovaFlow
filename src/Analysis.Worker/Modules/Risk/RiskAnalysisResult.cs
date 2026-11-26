using Anthropic.Helpers;
using InnovaFlow.Analysis.Data;
namespace Analysis.Worker.Modules.Risk;

// Field order is the order the AI writes in: the risks first, so the overall score is judged from them.
[SchemaClass("Systematic risk analysis of a user's idea, based only on the information provided. Do not invent exact financial losses, probabilities backed by fake statistics or legal conclusions. Everything in bulgarian!")]
public class RiskAnalysisResult
{
    [SchemaProperty("2-6 risks, ordered by priority (most serious first). Include only risk types that actually apply to this idea, at most one risk per Type. Concrete to this idea, not generic.")]
    public List<RiskResult> Risks { get; set; } = [];

    [SchemaProperty("Overall risk of the idea, integer 1-100 (100 is extremely risky, 1 is almost no risk). 1-33 = low, 34-66 = medium, 67-100 = high. Must be consistent with the probability, impact and severity of the risks listed above, weighted toward the most serious ones.", Minimum = 1, Maximum = 100)]
    public int RiskScore { get; set; }

    [SchemaProperty("2-3 sentences in Bulgarian (max ~300 characters): why the overall risk is at this level, naming the risks that drive it.")]
    public string RiskScoreExplanation { get; set; } = "";

    [SchemaProperty("3-5 sentences in Bulgarian (max ~600 characters): which risks block a start and which can be managed during a pilot, the most important actions before launch, and whether it is reasonable to start with a limited test. Mention any missing information that prevents a confident assessment.")]
    public string Summary { get; set; } = "";
}

public class RiskResult
{
    [SchemaProperty("Risk category: Market = demand and adoption; Financial = cost, funding, revenue; Technical = technology, integration, hardware, supply of components; Legal = regulation, certification, personal data; Security = data, cyber and physical safety; Competition = competitors and substitutes; Scalability = problems when growing.")]
    public RiskType Type { get; set; }

    [SchemaProperty("Short risk name in Bulgarian, max ~30 characters (e.g. 'Технически риск', 'Конкуренция').")]
    public string Title { get; set; } = "";

    [SchemaProperty("1-2 sentences in Bulgarian (max ~160 characters): what could happen and why, specific to this idea.")]
    public string Description { get; set; } = "";

    [SchemaProperty("Likelihood that the risk materialises, integer 1-100 (100 is almost certain). A reasoned estimate, not a statistic.", Minimum = 1, Maximum = 100)]
    public int Probability { get; set; }

    [SchemaProperty("Damage to the idea if the risk materialises, integer 1-100 (100 would make the idea fail). 1-33 = low, 34-66 = medium, 67-100 = high.", Minimum = 1, Maximum = 100)]
    public int Impact { get; set; }

    [SchemaProperty("Overall severity combining Probability and Impact. Low = both low; Medium = one of them medium or high; High = both high or one very high; Critical = could block the idea and needs to be solved before starting.")]
    public RiskSeverity Severity { get; set; }

    [SchemaProperty("Active = already relevant at the current stage and needs action now. Monitoring = possible later, to be watched during the pilot.")]
    public RiskStatus Status { get; set; }

    [SchemaProperty("Concrete measure to reduce the risk and how to check early that it works, 1-2 sentences in Bulgarian (max 180 characters). Realistic for the idea's stage.")]
    public string Mitigation { get; set; } = "";
}


public static class RiskMapping
{
    public static List<InnovaFlow.Analysis.Data.Risk> ToRiskEntities(this RiskAnalysisResult r) =>
        r.Risks.Select(x => new InnovaFlow.Analysis.Data.Risk
        {
            Type = x.Type,
            Title = x.Title,
            Description = x.Description,
            Probability = x.Probability,
            Impact = x.Impact,
            Severity = x.Severity,
            Status = x.Status,
            Mitigation = x.Mitigation
        }).ToList();
}
