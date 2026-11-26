using Client.Analyses;
using Contracts;
using Contracts.AnalysisDtos;
using Contracts.Ideas;

namespace Client.Ideas;

/// <summary>An idea with its analyses, as the dashboard and the idea list show it.</summary>
/// <param name="Latest">The newest analysis, whatever its state (null = never analysed).</param>
/// <param name="LastCompleted">The newest finished one: where the score and "Преглед" come from.</param>
public sealed record IdeaCard(IdeaDto Idea, AnalysisSummaryDto? Latest, AnalysisSummaryDto? LastCompleted)
{
    public bool IsRunning => Latest?.Status is AnalysisStatus.Pending or AnalysisStatus.Running;
    public bool LastFailed => Latest?.Status is AnalysisStatus.Failed;
    public int? Score => LastCompleted?.OverallScore;

    public DateTimeOffset LastActivity =>
        Latest?.CompletedAt ?? Latest?.StartedAt ?? Idea.UpdatedAt ?? Idea.CreatedAt;

    /// <summary>Where "Преглед" goes: the running analysis' progress, else the last result. Null = nothing to show.</summary>
    public string? ViewLink =>
        IsRunning ? $"start-analysis/{Latest!.Id}"
        : LastCompleted is { } done ? $"analyses/{done.Id}"
        : null;
}

/// <summary>Loads the user's ideas joined with their analyses (Projects + Analysis APIs).</summary>
public sealed class IdeaBoard(IdeaClient ideas, AnalysisClient analyses)
{
    /// <summary>Submitted ideas, most recently active first. Drafts are left out: the wizard can't resume them yet.</summary>
    /// <exception cref="HttpRequestException">Either API can't be reached.</exception>
    public async Task<List<IdeaCard>> LoadAsync(CancellationToken ct = default)
    {
        var ideasTask = ideas.GetIdeasAsync(ct);
        var analysesTask = analyses.GetAnalysesAsync(ct);
        await Task.WhenAll(ideasTask, analysesTask);

        var byIdea = analysesTask.Result
            .GroupBy(a => a.IdeaId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).ToList());

        return ideasTask.Result
            .Where(i => i.Status != IdeaStatus.Draft && !string.IsNullOrWhiteSpace(i.Title))
            .Select(i =>
            {
                var list = byIdea.GetValueOrDefault(i.Id) ?? [];
                return new IdeaCard(i,
                    list.FirstOrDefault(),
                    list.FirstOrDefault(a => a.Status == AnalysisStatus.Completed));
            })
            .OrderByDescending(c => c.LastActivity)
            .ToList();
    }
}

/// <summary>Display text shared by the idea pages.</summary>
public static class IdeaText
{
    public static (string Label, string Icon) Category(IdeaCategory c) => c switch
    {
        IdeaCategory.Business => ("Бизнес", "store"),
        IdeaCategory.Brand => ("Бранд", "branding_watermark"),
        IdeaCategory.Startup => ("Стартъп", "rocket_launch"),
        IdeaCategory.Product => ("Продукт", "inventory_2"),
        IdeaCategory.Service => ("Услуга", "handshake"),
        IdeaCategory.Application => ("Приложение", "apps"),
        IdeaCategory.SocialInitiative => ("Социална инициатива", "diversity_3"),
        IdeaCategory.Research => ("Изследване", "science"),
        _ => ("Друго", "category")
    };

    /// <summary>Theme colour for an overall score: primary ≥ 75, secondary ≥ 50, tertiary below.</summary>
    public static string ScoreColor(int? score) => score switch
    {
        >= 75 => "primary",
        >= 50 => "secondary",
        _ => "tertiary"
    };

    /// <summary>Same colours as hex, for SVG strokes (keep in step with tailwind.config.js).</summary>
    public static string ScoreHex(int? score) => ScoreColor(score) switch
    {
        "primary" => "#6bfb9a",
        "secondary" => "#5de6ff",
        _ => "#ffdd75"
    };

    public static string Potential(int score) => score switch
    {
        >= 75 => "Висок потенциал",
        >= 50 => "Среден потенциал",
        _ => "Нисък потенциал"
    };

    /// <summary>"Сега", "Преди 5 мин.", "Преди 2 ч.", "Вчера", "Преди 3 дни", then the date.</summary>
    public static string Ago(DateTimeOffset when)
    {
        var span = DateTimeOffset.UtcNow - when;
        if (span < TimeSpan.FromMinutes(1)) return "Сега";
        if (span < TimeSpan.FromHours(1)) return $"Преди {(int)span.TotalMinutes} мин.";
        if (span < TimeSpan.FromDays(1)) return $"Преди {(int)span.TotalHours} ч.";
        if (span < TimeSpan.FromDays(2)) return "Вчера";
        if (span < TimeSpan.FromDays(7)) return $"Преди {(int)span.TotalDays} дни";
        return when.ToLocalTime().ToString("dd.MM.yyyy");
    }
}
