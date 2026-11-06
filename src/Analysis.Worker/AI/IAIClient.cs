namespace Analysis.Worker.AI;

public interface IAiClient
{
    /// <summary>Structured answer, deserialized into T.</summary>
    Task<AiResult<T>> CompleteAsync<T>(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default) where T : class, new();

    /// <summary>Free-text answer (e.g. research notes). The text is in <see cref="AiResponse.Text"/>.</summary>
    Task<AiResponse> CompleteTextAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default);
}

/// <summary>A web page the model was shown. <see cref="Number"/> is what the model cites, e.g. [3].</summary>
public sealed record AiSource(int Number, string Title, string Url);

public sealed record AiResponse(
    string Text,
    string Model,
    string? StopReason,
    long InputTokens,
    long OutputTokens,
    IReadOnlyList<AiSource>? Sources,
    TimeSpan Duration)
{
    public long TotalTokens => InputTokens + OutputTokens;
    // Anthropic stop reasons / OpenAI-compatible finish reasons
    public bool WasTruncated => StopReason == "max_tokens" || StopReason == "length";
    public bool WasRefused => StopReason == "refusal" || StopReason == "content_filter";
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