namespace Analysis.Worker.Modules.Audience;

using System.Text;
using Anthropic.Helpers;

[SchemaClass("Target audience analysis of a user's idea: segments, personas and attractiveness scores. Everything is ")]
public sealed class AudienceAnalysisResult
{

    [SchemaProperty("The primary (most promising) audience segment")]
    public AudienceSegmentResult PrimarySegment { get; set; } = new();


    [SchemaProperty("1-3 secondary audience segments, only as far as the provided information allows")]
    public List<AudienceSegmentResult> SecondarySegments { get; set; } = [];

    // ── AudienceAnalyses.Description ────────────────────────────────

    [SchemaProperty("""
        Summary , short paragraphs separated by blank lines. Must contain, in this order:
        1) whether the user specified an audience; if not, that all segments are hypotheses;
        2) which segment looks best for a first test and why;
        3) the main uncertainties and which data (survey, interviews, analytics) would reduce them;
        4) a note that the personas are illustrative examples, not real interviewed people.
        """)]
    public string Description { get; set; } = "";

    // ── Personas ────────────────────────────────────────────────────

    [SchemaProperty("Up to 3 illustrative user personas Fictional examples, never presented as real people.")]
    public List<PersonaResult> Personas { get; set; } = [];

    // ── AnalysisScores (ScoreType.Audience) ─────────────────────────

    [SchemaProperty("Overall audience potential: how attractive and reachable the best segments are, integer 1-100", Minimum = 1, Maximum = 100)]
    public int OverallScore { get; set; }

    [SchemaProperty("Why OverallScore has this value and how confident you are (low/medium/high), 2-4 sentences")]
    public string OverallExplanation { get; set; } = "";

    /// <summary>Clamps scores and trims list sizes. The schema can't enforce ranges.</summary>
    public void Normalize()
    {
        OverallScore = Math.Clamp(OverallScore, 1, 100);
        PrimarySegment.Normalize();
        SecondarySegments = SecondarySegments.Take(3).ToList();
        SecondarySegments.ForEach(s => s.Normalize());
        Personas = Personas.Take(3).ToList();
    }
}

/// <summary>One audience segment. Formatted into a text column</summary>
public sealed class AudienceSegmentResult
{
    [SchemaProperty("Short segment name , e.g. 'Студенти, които често поръчват храна'")]
    public string Name { get; set; } = "";

    [SchemaProperty("True if the user did NOT name this segment and it is your hypothesis")]
    public bool IsHypothesis { get; set; }

    [SchemaProperty("The presumed need, 1-2 sentences .")]
    public string Need { get; set; } = "";

    [SchemaProperty("Motivation to use the solution, 1-2 sentences .")]
    public string Motivation { get; set; } = "";

    [SchemaProperty("Usage context: when, where and how they would use it, 1-2 sentences")]
    public string UsageContext { get; set; } = "";

    [SchemaProperty("Likely objections, 1-2 sentences .")]
    public string Objections { get; set; } = "";

    [SchemaProperty("Price sensitivity: exactly one of 'ниска', 'средна', 'висока', followed by a short reason")]
    public string PriceSensitivity { get; set; } = "";

    [SchemaProperty("Difficulty to reach: exactly one of 'ниска', 'средна', 'висока', followed by likely channels")]
    public string ReachDifficulty { get; set; } = "";

    [SchemaProperty("Segment attractiveness, integer 1-100.", Minimum = 1, Maximum = 100)]
    public int AttractivenessScore { get; set; }

    [SchemaProperty("Why AttractivenessScore has this value and the uncertainty (low/medium/high confidence), 1-2 sentences")]
    public string AttractivenessExplanation { get; set; } = "";

    public void Normalize() => AttractivenessScore = Math.Clamp(AttractivenessScore, 1, 100);

    /// <summary>Text saved into PrimaryAudience and SecondaryAudience.
    /// Analysis.Api parses it back (Endpoints/AudienceText.cs), so keep the labels in sync.</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("## ").Append(Name);
        if (IsHypothesis) sb.Append(" (хипотеза)");
        sb.AppendLine();
        sb.AppendLine($"Привлекателност: {AttractivenessScore}/100 — {AttractivenessExplanation}");
        sb.AppendLine($"Нужда: {Need}");
        sb.AppendLine($"Мотивация: {Motivation}");
        sb.AppendLine($"Контекст на използване: {UsageContext}");
        sb.AppendLine($"Възражения: {Objections}");
        sb.AppendLine($"Чувствителност към цена: {PriceSensitivity}");
        sb.Append($"Трудност за достигане: {ReachDifficulty}");
        return sb.ToString();
    }
}

public sealed class PersonaResult
{
    [SchemaProperty("Fictional first name and a short label ,e.g. 'Мария — заета студентка'.")]
    public string Name { get; set; } = "";

    [SchemaProperty("Approximate age range, e.g. '19-24'. Use 'не е посочено' if the input gives no basis for it. Never invent demographic facts")]
    public string AgeRange { get; set; } = "";

    [SchemaProperty("Occupation or role")]
    public string Occupation { get; set; } = "";

    [SchemaProperty("""
        Description , 2-4 sentences: which segment the persona belongs to, their motivation,
        their main objections, and the conditions that would make them try the solution.
        """)]
    public string Description { get; set; } = "";

    [SchemaProperty("2-4 goals, each a short phrase")]
    public List<string> Goals { get; set; } = [];

    [SchemaProperty("2-4 problems/pain points, each a short phrase")]
    public List<string> PainPoints { get; set; } = [];

    [SchemaProperty("2-4 relevant behaviours/habits, each a short phrase")]
    public List<string> Behaviors { get; set; } = [];
}