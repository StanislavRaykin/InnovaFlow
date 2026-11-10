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

    public int SearchResultsPerQuery { get; set; }

    public int MaxToolRounds { get; set; }

    /// <summary>
    /// Once this many seconds have passed, Kimi gets no more search rounds and must answer with what it has.
    /// The answer round itself still runs after this, so the total time is longer. 0 = no budget.
    /// </summary>
    public int SearchBudgetSeconds { get; set; }

    /// <summary>Retries for a web_search call that hits a rate limit (HTTP 429)</summary>
    public int SearchMaxRetries { get; set; } = 3;

    /// <summary>
    /// How many web_search requests may be in flight at once across the whole worker (all analyses).
    /// Kimi often asks for several searches in one turn; firing them all at once trips Moonshot's rate limit.
    /// </summary>
    public int MaxConcurrentSearches { get; set; } = 2;
}
