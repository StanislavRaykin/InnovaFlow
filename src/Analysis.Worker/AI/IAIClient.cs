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
        CancellationToken ct = default) where T : class, new();
}

public sealed record AiResponse(
    string Provider,
    string Text,
    string Model,
    string? StopReason,
    long InputTokens,
    long OutputTokens,
    TimeSpan Duration)
{
    public long TotalTokens => InputTokens + OutputTokens;
    public bool WasTruncated => StopReason == "max_tokens";
    public bool WasRefused => StopReason == "refusal";
}


/// <summary>Carries token usage and the model name so AIRequests can be filled in.</summary>
public record AiResult<T>(T Value, AiResponse Raw) where T : class;

public class AiClientException(string message, Exception? inner = null, AiResponse? response = null)
    : Exception(message, inner)
{
    public AiResponse? Response { get; } = response;
}





public class AIClientFactory(IServiceProvider sp) : IAIClientFactory
{
    public IAiClient GetAiClient(string clientName) =>
        sp.GetKeyedService<IAiClient>(clientName) ?? throw new InvalidOperationException($"No AI client registered as '{clientName}'");
}

public interface IAIClientFactory
{
    IAiClient GetAiClient(string clientName);
}