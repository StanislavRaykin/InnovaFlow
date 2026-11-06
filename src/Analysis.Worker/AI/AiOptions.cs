namespace Analysis.Worker.AI;

public class AiOptions
{
    public string? BaseUrl { get; set; }

    public int MaxTokens { get; set; }

    public int MaxRetries { get; set; } = 3;

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 120;
}

public class ClaudeOptions : AiOptions;

public class KimiOptions : AiOptions
{
    public string ReasoningEffort { get; set; } = "";

    /// <summary>Results per web_search call (Moonshot search_pro allows 1-20).</summary>
    public int SearchResultsPerQuery { get; set; }

    /// <summary>How many search rounds Kimi can run before it must answer.</summary>
    public int MaxToolRounds { get; set; }

    /// <summary>Retries for a web_search call that hits a rate limit (HTTP 429). Other errors aren't retried.</summary>
    public int SearchMaxRetries { get; set; } = 3;
}
//more will come