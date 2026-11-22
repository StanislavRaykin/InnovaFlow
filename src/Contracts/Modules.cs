namespace Contracts;

public static class AnalysisModules
{
    /// <summary>
    /// Key is the name of the module and Value is what it does
    /// </summary> 
   public static readonly IReadOnlyDictionary<string, string> All =
    new Dictionary<string, string>
    {
        ["Classifier"]      = "Обобщение на вашата идея",
        ["Audience"]        = "Идентифициране на целева аудитория",
        ["Market"]          = "Анализ на пазара",
        ["Competitor"]      = "Намиране на конкуренти",
        ["Innovation"]      = "Оценяване на иновацията",
        ["Feasibility"]     = "Оценка на осъществимостта",
        ["Risk"]            = "Идентифициране на рисковете",
        ["Recommendations"] = "Генериране на препоръки",
    }.AsReadOnly();
}
