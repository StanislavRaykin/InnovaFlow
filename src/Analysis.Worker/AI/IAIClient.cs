namespace Analysis.Worker.AI;

public interface IAiClient
{
    /// <summary>
    /// Sends a prompt and deserializes the reply into T. The implementation is responsible
    /// for forcing JSON output and for stripping markdown fences if the model adds them.
    /// </summary>
    Task<AiResult<T>> CompleteAsync<T>(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default) where T : class;
}

/// <summary>Carries token usage and the model name so AIRequests can be filled in.</summary>
public record AiResult<T>(T Value, string Model, int TokensUsed) where T : class;
