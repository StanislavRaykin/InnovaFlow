namespace Contracts;

/// <param name="Key">Module name, as IAnalysisModule.Name in the worker.</param>
/// <param name="Title">What it does, shown while the analysis runs.</param>
/// <param name="Name">Short name of its result, e.g. in the "done" notification.</param>
/// <param name="Path">Its result tab: /analyses/{id}/{Path}.</param>
public record ModuleInfo(string Key, string Title, string? MiIcon, string Name, string Path);
public static class AnalysisModules
{
    /// <summary>
    /// Key is the name of the module and Value is what it does
    /// </summary>
  public static readonly IReadOnlyList<ModuleInfo> All =
    [
        // new("Classifier", "Разбиране на вашата идея", null),
        new("Audience", "Идентифициране на целева аудитория", "groups", "Целева аудитория", "audience"),
        new("Market",  "Анализ на пазара",                   "trending_up", "Пазарен анализ", "market"),
        // new("Competitor", "Намиране на конкуренти",             null),
        new("Innovation",  "Оценяване на иновацията",            "tips_and_updates", "Иновации", "innovation"),
        new("Feasibility", "Оценка на осъществимостта",          "engineering", "Осъществимост", "feasibility"),
        new("Risk",  "Идентифициране на рисковете",              "warning", "Рискове", "risks"),
        // new("Recommendations", "Генериране на препоръки", null),
    ];

    public static ModuleInfo? Find(string key) => All.FirstOrDefault(m => m.Key == key);
}
