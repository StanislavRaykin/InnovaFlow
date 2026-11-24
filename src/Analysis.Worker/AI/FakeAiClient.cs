using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Analysis.Worker.AI;

/// <summary>Settings for <see cref="FakeAiClient"/>, bound from <c>Ai:Fake</c></summary>
public class FakeAiOptions
{
    /// <summary>How long a structured answer takes to stream, in seconds. 0 = instant.</summary>
    public double StreamSeconds { get; set; } = 6;

    /// <summary>How long a text (research) answer takes, fake searches included, in seconds.</summary>
    public double ResearchSeconds { get; set; } = 8;

    /// <summary>Fake web searches per text answer. Each one adds two example.com sources.</summary>
    public int Searches { get; set; } = 3;

    /// <summary>0-1 chance that a call fails partway through with <see cref="AiClientException"/>, to test failure states.</summary>
    public double FailureRate { get; set; }
}

/// <summary>
/// Free stand-in for the real AI clients, used when <c>Ai:UseFake</c> is true. Makes no network calls.
/// <para>
/// Structured answers are dummy instances of the requested type, built by <see cref="FakeData"/> from the same
/// [SchemaProperty] attributes the real schema comes from. They are serialized like the model's JSON, streamed back in
/// small chunks with delays, and parsed by the SDK, so partial previews, progress events and parsing run exactly as
/// with a real model. Text answers (research) emit a few <see cref="AiWebSearch"/>es and return notes citing numbered
/// example.com sources.
/// </para>
/// Output is seeded from the prompt, so the same idea gets the same dummy result on a rerun.
/// </summary>
public sealed class FakeAiClient(string name, FakeAiOptions options, ILogger<FakeAiClient> logger) : IAiClient
{
    // Same naming as Anthropic.Helpers.StructuredOutput.Parse, and Cyrillic written as-is instead of \uXXXX escapes
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string[] SearchTopics = ["размер на пазара", "конкуренти", "цени", "тенденции 2026", "потребители"];

    // Ends up in AIRequests.Model, so dummy rows are easy to tell apart from real ones
    private string Model => $"fake-{name.ToLowerInvariant()}";

    public Task<AiResult<T>> CompleteAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
        => AiStream.CollectAsync<T>(StreamAsync<T>(systemPrompt, userPrompt, ct), ct);

    public Task<AiResponse> CompleteTextAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => AiStream.CollectAsync(StreamTextAsync(systemPrompt, userPrompt, ct), ct);

    // ParseCompleted runs the dummy JSON through the SDK parser, same as the real clients
    public IAsyncEnumerable<AiStreamEvent> StreamAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
        => AiStream.ParseCompleted<T>(StreamJsonAsync<T>(systemPrompt, userPrompt, ct), Model, logger, ct);

    public async IAsyncEnumerable<AiStreamEvent> StreamTextAsync(string systemPrompt, string userPrompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var rng = new Random(Seed(userPrompt, "text"));
        var title = IdeaTitle(userPrompt);
        var searches = Math.Max(0, options.Searches);
        logger.LogInformation("{Model} faking research with {Searches} searches", Model, searches);

        // Like Kimi: text written before a tool call isn't the answer and is dropped with a reset
        yield return new AiTextDelta("Ще потърся актуална информация...");
        yield return new AiTextReset();

        var sources = new List<AiSource>();
        var searchDelay = TimeSpan.FromSeconds(Math.Max(0, options.ResearchSeconds) * 0.6 / Math.Max(1, searches));
        for (var i = 0; i < searches; i++)
        {
            var topic = SearchTopics[i % SearchTopics.Length];
            yield return new AiWebSearch($"{title} {topic}");
            await Task.Delay(searchDelay, ct);

            for (var j = 0; j < 2; j++)
            {
                var n = sources.Count + 1;
                sources.Add(new AiSource(n, $"Тестов източник {n}: {topic}", $"https://example.com/research/{n}"));
            }
        }

        var text = FakeData.ResearchNotes(title, sources, rng);
        await foreach (var e in StreamChunksAsync(text, options.ResearchSeconds * 0.4, rng, systemPrompt, userPrompt, sw, ct))
            yield return e;

        yield return new AiStreamCompleted(Response(text, systemPrompt, userPrompt, sources, sw, "end_turn"));
    }

    private async IAsyncEnumerable<AiStreamEvent> StreamJsonAsync<T>(string systemPrompt, string userPrompt,
        [EnumeratorCancellation] CancellationToken ct) where T : class, new()
    {
        var sw = Stopwatch.StartNew();
        var rng = new Random(Seed(userPrompt, typeof(T).FullName ?? typeof(T).Name));
        var json = JsonSerializer.Serialize(FakeData.Create<T>(rng), Json);
        logger.LogInformation("{Model} streaming a dummy {Type} ({Chars} chars)", Model, typeof(T).Name, json.Length);

        await foreach (var e in StreamChunksAsync(json, options.StreamSeconds, rng, systemPrompt, userPrompt, sw, ct))
            yield return e;

        yield return new AiStreamCompleted(Response(json, systemPrompt, userPrompt, null, sw, "end_turn"));
    }

    /// <summary>Streams <paramref name="text"/> in token-sized chunks spread over <paramref name="seconds"/>, failing partway if FailureRate says so.</summary>
    private async IAsyncEnumerable<AiStreamEvent> StreamChunksAsync(string text, double seconds, Random rng,
        string systemPrompt, string userPrompt, Stopwatch sw, [EnumeratorCancellation] CancellationToken ct)
    {
        var chunks = Chunk(text, rng);
        var delay = TimeSpan.FromSeconds(Math.Max(0, seconds) / Math.Max(1, chunks.Count));
        // not seeded, so a rerun of the same idea can succeed
        var failAt = Random.Shared.NextDouble() < options.FailureRate ? Random.Shared.Next(chunks.Count) : -1;

        for (var i = 0; i < chunks.Count; i++)
        {
            if (i == failAt)
            {
                logger.LogWarning("{Model} simulating a failure after {Chunks}/{Total} chunks", Model, i, chunks.Count);
                var partial = string.Concat(chunks.Take(i));
                throw new AiClientException($"{Model}: simulated failure (Ai:Fake:FailureRate = {options.FailureRate})",
                    response: Response(partial, systemPrompt, userPrompt, null, sw, stopReason: null));
            }

            await Task.Delay(delay, ct);
            yield return new AiTextDelta(chunks[i]);
        }
    }

    private AiResponse Response(string text, string systemPrompt, string userPrompt, IReadOnlyList<AiSource>? sources, Stopwatch sw, string? stopReason)
        => new(text, Model, stopReason,
            InputTokens: (systemPrompt.Length + userPrompt.Length) / 4,   // rough chars-per-token guess, just so the columns aren't 0
            OutputTokens: text.Length / 4,
            sources, sw.Elapsed);

    private static List<string> Chunk(string text, Random rng)
    {
        var chunks = new List<string>();
        for (var i = 0; i < text.Length;)
        {
            var len = Math.Min(rng.Next(6, 24), text.Length - i);
            if (char.IsHighSurrogate(text[i + len - 1]) && i + len < text.Length) len++;   // don't split a surrogate pair
            chunks.Add(text.Substring(i, len));
            i += len;
        }
        return chunks;
    }

    /// <summary>Stable across processes, unlike string.GetHashCode</summary>
    private static int Seed(string a, string b)
    {
        unchecked
        {
            var h = 2166136261u;   // FNV-1a
            foreach (var c in a + "\0" + b) { h ^= c; h *= 16777619u; }
            return (int)h;
        }
    }

    // IdeaPromptBuilder writes "Име на идеята: ..."
    private static readonly Regex IdeaTitleRegex = new(@"^Име на идеята:\s*(.+)$", RegexOptions.Multiline);

    private static string IdeaTitle(string userPrompt)
        => IdeaTitleRegex.Match(userPrompt) is { Success: true } m ? m.Groups[1].Value.Trim() : "идеята";
}
