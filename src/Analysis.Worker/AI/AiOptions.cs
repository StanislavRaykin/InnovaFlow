namespace Analysis.Worker.AI;

public class AiOptions
{
    public string? BaseUrl { get; set; }

    public int MaxTokens { get; set; }

    public int MaxRetries { get; set; } = 2;

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 120;
}

public class ClaudeOptions : AiOptions;
//more will come