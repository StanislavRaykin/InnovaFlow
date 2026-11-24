using Contracts.AnalysisDtos;

namespace Analysis.Api.Endpoints;

/// <summary>
/// Reads back the segment text the worker saves into AudienceAnalyses.PrimaryAudience / SecondaryAudience
/// (AudienceSegmentResult.ToText() in Analysis.Worker). The labels below must match it.
/// <code>
/// ## Name (хипотеза)
/// Привлекателност: 75/100 — explanation
/// Нужда: …
/// …
/// </code>
/// SecondaryAudience holds several of these joined by a blank line.
/// </summary>
internal static class AudienceText
{
    private const string HypothesisSuffix = " (хипотеза)";

    private const string Attractiveness = "Привлекателност";
    private const string Need = "Нужда";
    private const string Motivation = "Мотивация";
    private const string UsageContext = "Контекст на използване";
    private const string Objections = "Възражения";
    private const string PriceSensitivity = "Чувствителност към цена";
    private const string ReachDifficulty = "Трудност за достигане";

    private static readonly string[] Labels =
        [Attractiveness, Need, Motivation, UsageContext, Objections, PriceSensitivity, ReachDifficulty];

    public static List<AudienceSegmentDto> ParseSegments(string? text)
    {
        var segments = new List<AudienceSegmentDto>();
        if (string.IsNullOrWhiteSpace(text)) return segments;

        string? header = null;
        var fields = new Dictionary<string, string>();
        string? lastLabel = null;

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');

            if (line.StartsWith("## "))
            {
                if (header is not null) segments.Add(Build(header, fields));
                header = line[3..];
                fields = [];
                lastLabel = null;
                continue;
            }
            if (header is null) continue;

            var label = Labels.FirstOrDefault(l => line.StartsWith(l + ": "));
            if (label is not null)
            {
                fields[label] = line[(label.Length + 2)..];
                lastLabel = label;
            }
            else if (lastLabel is not null)
            {
                fields[lastLabel] += "\n" + line;   // a value that spans several lines
            }
        }
        if (header is not null) segments.Add(Build(header, fields));
        return segments;
    }

    private static AudienceSegmentDto Build(string header, Dictionary<string, string> fields)
    {
        var isHypothesis = header.EndsWith(HypothesisSuffix);
        var name = isHypothesis ? header[..^HypothesisSuffix.Length] : header;

        // "75/100 — explanation"
        int? score = null;
        string? explanation = null;
        if (fields.TryGetValue(Attractiveness, out var a))
        {
            var parts = a.Split(" — ", 2);
            var slash = parts[0].IndexOf('/');
            if (int.TryParse(slash < 0 ? parts[0] : parts[0][..slash], out var s))
                score = Math.Clamp(s, 1, 100);
            explanation = parts.Length > 1 ? Clean(parts[1]) : null;
        }

        return new AudienceSegmentDto(
            name.Trim(),
            isHypothesis,
            Get(Need),
            Get(Motivation),
            Get(UsageContext),
            Get(Objections),
            Get(PriceSensitivity),
            Get(ReachDifficulty),
            score,
            explanation);

        string? Get(string label) => fields.TryGetValue(label, out var v) ? Clean(v) : null;
    }

    private static string? Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
