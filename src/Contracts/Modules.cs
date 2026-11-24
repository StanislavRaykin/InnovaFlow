namespace Contracts;

public record ModuleInfo(string Key, string Title, string? MiIcon);
public static class AnalysisModules
{
    /// <summary>
    /// Key is the name of the module and Value is what it does
    /// </summary> 
  public static readonly IReadOnlyList<ModuleInfo> All =
    [
        // new("Classifier", "Разбиране на вашата идея", null),
        new("Audience", "Идентифициране на целева аудитория", "groups"),
        new("Market",  "Анализ на пазара",                   "trending_up"),
        // new("Competitor", "Намиране на конкуренти",             null),
        new("Innovation",  "Оценяване на иновацията",            "tips_and_updates"),
        // new("Feasibility", "Оценка на осъществимостта",          "Feasibility Agent"),
        // new("Risk",  "Идентифициране на рисковете", "Risk Agent"),
        // new("Recommendations", "Генериране на препоръки", null),
    ];
}
