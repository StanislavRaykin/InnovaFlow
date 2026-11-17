using System.Runtime.CompilerServices;
using Anthropic.Helpers;

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

    /// <summary>
    /// Free-text answer, streamed as it is generated. The last event is always <see cref="AiStreamCompleted"/>,
    /// whose response holds the full text (the <see cref="AiTextDelta"/>s since the last <see cref="AiTextReset"/>, joined).
    /// Failures throw <see cref="AiClientException"/> while enumerating, same as <see cref="CompleteTextAsync"/>.
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamTextAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default);

    /// <summary>
    /// Structured answer, streamed as it is generated. The <see cref="AiTextDelta"/>s are raw chunks of the JSON
    /// (run the joined text through <see cref="PartialJson"/> for a live preview). The last event is always
    /// <see cref="AiStreamCompleted{T}"/> with the parsed value. Failures, including JSON that doesn't parse
    /// into <typeparamref name="T"/>, throw <see cref="AiClientException"/> while enumerating.
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamAsync<T>(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default) where T : class, new();
}

/// <summary>One piece of a streamed answer</summary>
public record AiStreamEvent;

/// <summary>The next chunk of a text answer</summary>
public sealed record AiTextDelta(string Text) : AiStreamEvent;

/// <summary>
/// The text streamed so far was not the answer (the model wrote it before calling a tool). Drop it: the answer
/// starts over with the next <see cref="AiTextDelta"/>. Only clients with a tool loop emit this.
/// </summary>
public sealed record AiTextReset : AiStreamEvent;

/// <summary>The model started a web search (only clients that search emit this)</summary>
public sealed record AiWebSearch(string Query) : AiStreamEvent;

/// <summary>The stream finished. Always the last event.</summary>
public record AiStreamCompleted(AiResponse Response) : AiStreamEvent;

/// <summary>A structured stream finished and its JSON parsed into <typeparamref name="T"/>. Always the last event.</summary>
public sealed record AiStreamCompleted<T>(T Value, AiResponse Response) : AiStreamCompleted(Response) where T : class;

public static class AiStream
{
    /// <summary>Drains a stream and returns its final response.</summary>
    public static async Task<AiResponse> CollectAsync(IAsyncEnumerable<AiStreamEvent> stream, CancellationToken ct = default)
        => (await CollectCompletedAsync(stream, ct)).Response;

    /// <summary>Drains a structured stream (<see cref="IAiClient.StreamAsync{T}"/>) and returns its parsed value.</summary>
    public static async Task<AiResult<T>> CollectAsync<T>(IAsyncEnumerable<AiStreamEvent> stream, CancellationToken ct = default) where T : class
    {
        var done = await CollectCompletedAsync(stream, ct);
        if (done is not AiStreamCompleted<T> typed)
            throw new AiClientException($"AI stream did not end with a parsed {typeof(T).Name}", response: done.Response);
        return new AiResult<T>(typed.Value, typed.Response);
    }

    /// <summary>
    /// Forwards a JSON-mode stream unchanged, except the final <see cref="AiStreamCompleted"/>: its text is parsed
    /// into <typeparamref name="T"/> and it goes out as <see cref="AiStreamCompleted{T}"/>.
    /// </summary>
    public static async IAsyncEnumerable<AiStreamEvent> ParseCompleted<T>(IAsyncEnumerable<AiStreamEvent> stream,
        string clientName, ILogger logger, [EnumeratorCancellation] CancellationToken ct = default) where T : class, new()
    {
        await foreach (var e in stream.WithCancellation(ct))
        {
            if (e is not AiStreamCompleted done)
            {
                yield return e;
                continue;
            }

            T value;
            try
            {
                value = StructuredOutput.Parse<T>(done.Response.Text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse {Client} response. Response text: {ResponseText}", clientName, done.Response.Text);
                throw new AiClientException($"Failed to parse {clientName} response", ex, done.Response);
            }

            yield return new AiStreamCompleted<T>(value, done.Response);
        }
    }

    private static async Task<AiStreamCompleted> CollectCompletedAsync(IAsyncEnumerable<AiStreamEvent> stream, CancellationToken ct)
    {
        await foreach (var e in stream.WithCancellation(ct))
        {
            if (e is AiStreamCompleted done)
                return done;
        }
        throw new AiClientException("AI stream ended without a final response");
    }
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


/// <summary>Carries token usage and the model name so AIRequests can be filled in</summary>
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