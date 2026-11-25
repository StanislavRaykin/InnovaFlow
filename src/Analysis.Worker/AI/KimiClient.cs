using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using Anthropic.Helpers;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Analysis.Worker.AI;

/// <summary>
/// Kimi (Moonshot) through its OpenAI-compatible Chat Completions API, with a <c>web_search</c> tool
/// backed by Moonshot's Web Search Pro API (<c>/v1/tools/search_pro</c>).
/// <para>
/// Search results are numbered here ([1], [2], ...) before Kimi sees them, so its citations map to
/// <see cref="AiResponse.Sources"/>. (Moonshot's built-in web-search formula returns encrypted results,
/// so the URLs behind Kimi's citations would be unreadable.)
/// </para>
/// <para>
/// Uses the OpenAI SDK's protocol methods (raw JSON) instead of the typed ChatMessage models: K3 always
/// thinks and expects every assistant message, including its <c>reasoning_content</c>, to be sent back
/// unchanged inside a tool-call loop. The typed models would drop that field.
/// </para>
/// </summary>
public  class KimiClient : IAiClient
{
    private const string DefaultBaseUrl = "https://api.moonshot.ai/v1";
    private const string WebSearchToolName = "web_search";
    private const int MaxCharsPerResult = 1500;

    private const string SearchPolicy = """

    Search policy: issue ALL web_search calls together in your first reply (up to 5 in parallel),
    then answer. Do not search again after the results arrive.
    """;

    // private string additionalPromp = "Make all researches in the first round"

    private const string WebSearchToolJson = """
        [{
          "type": "function",
          "function": {
            "name": "web_search",
            "description": "Search the web. Returns numbered results with title, URL and relevant excerpts. Numbers stay the same across searches.",
            "parameters": {
              "type": "object",
              "properties": {
                "query": { "type": "string", "description": "Specific search query: an entity, a qualifier and, if relevant, a time term." }
              },
              "required": ["query"]
            }
          }
        }]
        """;

    private readonly ChatClient _chat;
    private readonly ClientPipeline _searchPipeline;
    // The client is a singleton, so this throttles searches across every analysis running in the worker.
    private readonly SemaphoreSlim _searchSlots;
    private readonly KimiOptions _options;
    private readonly ILogger<KimiClient> _logger;
    private readonly string _baseUrl;

    public KimiClient(IOptions<KimiOptions> options, ILogger<KimiClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            throw new ArgumentNullException("Kimi API key not found. Check AppHost");
        }

        _baseUrl = (string.IsNullOrWhiteSpace(_options.BaseUrl) ? DefaultBaseUrl : _options.BaseUrl).TrimEnd('/');

        var credential = new ApiKeyCredential(_options.ApiKey);

        _chat = new ChatClient(_options.Model, credential, new OpenAIClientOptions
        {
            Endpoint = new Uri(_baseUrl),
            NetworkTimeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
            RetryPolicy = new ClientRetryPolicy(_options.MaxRetries)
        });

        int slots = Math.Max(1, _options.MaxConcurrentSearches);
        _searchSlots = new SemaphoreSlim(slots, slots);

        // Searches get their own pipeline so they retry on rate limits (429) only.
        _searchPipeline = ClientPipeline.Create(
            new ClientPipelineOptions
            {
                NetworkTimeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                RetryPolicy = new RateLimitRetryPolicy(_options.SearchMaxRetries, _logger)
            },
            perCallPolicies: [ApiKeyAuthenticationPolicy.CreateBearerAuthorizationPolicy(credential)],
            perTryPolicies: [],
            beforeTransportPolicies: []);
    }

    public IAsyncEnumerable<AiStreamEvent> StreamTextAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => StreamCoreAsync(systemPrompt, userPrompt, responseFormat: null, ct);

    public Task<AiResponse> CompleteTextAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => AiStream.CollectAsync(StreamTextAsync(systemPrompt, userPrompt, ct), ct);

    // With a json_schema response_format the content deltas are the JSON itself, streamed as it is generated.
    public IAsyncEnumerable<AiStreamEvent> StreamAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
    {
        var responseFormat = new JsonObject
        {
            ["type"] = "json_schema",
            ["json_schema"] = new JsonObject
            {
                ["name"] = typeof(T).Name,
                ["strict"] = true,
                ["schema"] = KimiSchema.From<T>()
            }
        };

        return AiStream.ParseCompleted<T>(StreamCoreAsync(systemPrompt, userPrompt, responseFormat, ct), "Kimi", _logger, ct);
    }

    public Task<AiResult<T>> CompleteAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
        => AiStream.CollectAsync<T>(StreamAsync<T>(systemPrompt, userPrompt, ct), ct);

    /// <summary>
    /// The chat + web_search loop shared by both modes, streamed. <paramref name="responseFormat"/> null = free text.
    /// Every round is a streamed request; its text is forwarded as it arrives and the round's assistant message
    /// (content, reasoning_content, tool_calls) is rebuilt from the deltas to go back into the history.
    /// The answer is only the last round's text: text from a round that ends in tool calls is dropped with an <see cref="AiTextReset"/>.
    /// </summary>
    private async IAsyncEnumerable<AiStreamEvent> StreamCoreAsync(string systemPrompt, string userPrompt, JsonObject? responseFormat,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (userPrompt == null)
            throw new ArgumentException("User prompt cannot be null", nameof(userPrompt));

        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = systemPrompt + SearchPolicy },
            new JsonObject { ["role"] = "user", ["content"] = userPrompt }
        };

        // Totals across every round of the tool loop: all of it is billed.
        long inputTokens = 0, outputTokens = 0;
        string model = _options.Model;
        string? stopReason = null;
        var text = new StringBuilder();
        int searches = 0;
        var sources = new SourceCollector();

        Stopwatch sw = Stopwatch.StartNew();
        AiResponse Snapshot() => new(text.ToString(), model, stopReason, inputTokens, outputTokens, sources.ToList(), sw.Elapsed);

        for (int round = 0; ; round++)
        {
            // Out of search rounds or time: keep the tool declared (history contains tool calls) but forbid new calls.
            bool overBudget = _options.SearchBudgetSeconds > 0 && sw.Elapsed.TotalSeconds >= _options.SearchBudgetSeconds;
            bool lastRound = round >= _options.MaxToolRounds || overBudget;
            if (overBudget && round < _options.MaxToolRounds)
                _logger.LogInformation("Kimi search budget ({Budget}s) used up after {Rounds} rounds, forcing an answer", _options.SearchBudgetSeconds, round);

            var roundStart = sw.Elapsed;

            

            var body = new JsonObject
            {
                ["model"] = _options.Model,
                ["messages"] = messages.DeepClone(),
                ["max_completion_tokens"] = _options.MaxTokens,
                ["reasoning_effort"] = _options.ReasoningEffort,
                ["tools"] = JsonNode.Parse(WebSearchToolJson),
                ["tool_choice"] = lastRound ? "none" : "auto",
                ["stream"] = true,
                ["stream_options"] = new JsonObject { ["include_usage"] = true }
            };
            if (responseFormat is not null)
                body["response_format"] = responseFormat.DeepClone();

            ClientResult result;
            try
            {
                // BufferResponse = false: the body is read as it arrives instead of after the whole answer.
                result = await _chat.CompleteChatAsync(
                    BinaryContent.Create(BinaryData.FromString(body.ToJsonString())),
                    new RequestOptions { CancellationToken = ct, BufferResponse = false });
            }
            catch (ClientResultException ex)
            {
                var errorBody = ex.GetRawResponse()?.Content?.ToString();
                _logger.LogError(ex, "Kimi API error occurred. Status: {Status}. Body: {Body}", ex.Status, errorBody);
                throw new AiClientException($"Kimi API error occurred (HTTP {ex.Status})", ex, Snapshot());
            }

            using PipelineResponse rawResponse = result.GetRawResponse();
            Stream responseStream = rawResponse.ContentStream
                ?? throw new AiClientException("Kimi returned an empty response", response: Snapshot());

            var streamed = new StreamedRound();
            await using (var events = SseParser.Create(responseStream).EnumerateAsync(ct).GetAsyncEnumerator(ct))
            {
                while (true)
                {
                    // yield can't sit inside a try/catch, so only the read is wrapped.
                    string data;
                    try
                    {
                        if (!await events.MoveNextAsync())
                            break;
                        data = events.Current.Data;
                    }
                    catch (Exception ex) when (ex is IOException or HttpRequestException)
                    {
                        _logger.LogError(ex, "Kimi stream broke off in round {Round}", round);
                        throw new AiClientException("Kimi stream broke off", ex, Snapshot());
                    }

                    if (data == "[DONE]")
                        break;

                    var chunk = JsonNode.Parse(data);
                    if (chunk is null)
                        continue;
                    if (chunk["error"] is JsonNode error)
                        throw new AiClientException($"Kimi stream error: {error.ToJsonString()}", response: Snapshot());

                    string? delta = streamed.Add(chunk);
                    if (!string.IsNullOrEmpty(delta))
                    {
                        text.Append(delta);
                        yield return new AiTextDelta(delta);
                    }
                }
            }

            inputTokens += streamed.InputTokens;
            outputTokens += streamed.OutputTokens;
            model = streamed.Model ?? model;
            stopReason = streamed.FinishReason;

            _logger.LogInformation("Kimi round {Round}: model call took {Duration:0.0}s, finish {Finish}, {ToolCalls} tool calls, {Out} output tokens",
                round, (sw.Elapsed - roundStart).TotalSeconds, stopReason, streamed.ToolCalls.Count, streamed.OutputTokens);

            if (stopReason != "tool_calls" || streamed.ToolCalls.Count == 0)
                break;

            if (lastRound)
                throw new AiClientException($"Kimi kept calling tools after {_options.MaxToolRounds} rounds", response: Snapshot());

            // The full assistant message (with reasoning_content and tool_calls) goes back into the history.
            messages.Add(streamed.ToAssistantMessage());

            // Text written before tool calls isn't the answer; in JSON mode it would also break the JSON.
            if (text.Length > 0)
            {
                text.Clear();
                yield return new AiTextReset();
            }

            foreach (var call in streamed.ToolCalls)
            {
                if (call.Name == WebSearchToolName && GetQuery(call.Arguments) is { } query)
                    yield return new AiWebSearch(query);
            }

            // Searches run in parallel; numbering happens afterwards, in tool-call order, so it's deterministic.
            var searchStart = sw.Elapsed;
            var outcomes = await Task.WhenAll(streamed.ToolCalls.Select(tc => RunToolCallAsync(tc, ct)));
            _logger.LogInformation("Kimi round {Round}: {Count} searches took {Duration:0.0}s",
                round, streamed.ToolCalls.Count, (sw.Elapsed - searchStart).TotalSeconds);
            foreach (var outcome in outcomes)
            {
                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = outcome.ToolCallId,
                    ["name"] = outcome.Name,
                    ["content"] = outcome.Error ?? FormatResults(outcome.Hits!, sources)
                });
            }

            searches += streamed.ToolCalls.Count;
        }

        AiResponse response = Snapshot();
        _logger.LogInformation("Kimi finished after {Searches} web searches ({Sources} sources) in {Duration}. Tokens in/out: {In}/{Out}",
            searches, response.Sources?.Count ?? 0, response.Duration, response.InputTokens, response.OutputTokens);

        if (response.WasRefused)
        {
            _logger.LogWarning("Kimi refused to answer the prompt. StopReason: {StopReason}", response.StopReason);
            throw new AiClientException($"Kimi refused to answer the prompt. StopReason: {response.StopReason}", response: response);
        }

        if (response.WasTruncated)
        {
            _logger.LogWarning("Kimi hit max tokens usage. StopReason: {StopReason}", response.StopReason);
            throw new AiClientException($"Kimi hit max tokens usage. StopReason: {response.StopReason}", response: response);
        }

        yield return new AiStreamCompleted(response);
    }

    private record ToolCall(string Id, string Name, string Arguments);

    /// <summary>
    /// One streamed round, put back together from its chunks. Usage can arrive both top-level and inside the
    /// choice (Moonshot sends the same totals in both), so it's overwritten, never summed.
    /// </summary>
    private sealed class StreamedRound
    {
        private sealed class ToolCallBuilder
        {
            public string? Id;
            public string? Name;
            public readonly StringBuilder Arguments = new();
        }

        private readonly StringBuilder _content = new();
        private readonly StringBuilder _reasoning = new();
        private bool _hasReasoning;
        // Keyed by the delta's "index", so tool calls keep the order the model gave them.
        private readonly SortedDictionary<int, ToolCallBuilder> _toolCalls = new();

        public string? Model { get; private set; }
        public string? FinishReason { get; private set; }
        public long InputTokens { get; private set; }
        public long OutputTokens { get; private set; }

        public IReadOnlyList<ToolCall> ToolCalls => _toolCalls.Values
            .Select(b => new ToolCall(b.Id ?? "", b.Name ?? "", b.Arguments.Length == 0 ? "{}" : b.Arguments.ToString()))
            .ToList();

        /// <summary>Takes in one chunk; returns its answer text, if it has any.</summary>
        public string? Add(JsonNode chunk)
        {
            Model = AsText(chunk["model"]) ?? Model;
            ReadUsage(chunk["usage"]);

            if (chunk["choices"] is not JsonArray { Count: > 0 } choices || choices[0] is not JsonObject choice)
                return null;

            ReadUsage(choice["usage"]);
            FinishReason = AsText(choice["finish_reason"]) ?? FinishReason;

            if (choice["delta"] is not JsonObject delta)
                return null;

            if (delta.ContainsKey("reasoning_content"))
            {
                _hasReasoning = true;
                _reasoning.Append(AsText(delta["reasoning_content"]));
            }

            if (delta["tool_calls"] is JsonArray calls)
            {
                foreach (var call in calls)
                {
                    if (call is null)
                        continue;
                    int index = call["index"]?.GetValue<int>() ?? _toolCalls.Count;
                    if (!_toolCalls.TryGetValue(index, out var builder))
                        _toolCalls[index] = builder = new ToolCallBuilder();

                    builder.Id ??= AsText(call["id"]);
                    builder.Name ??= AsText(call["function"]?["name"]);
                    builder.Arguments.Append(AsText(call["function"]?["arguments"]));
                }
            }

            string? content = AsText(delta["content"]);
            if (!string.IsNullOrEmpty(content))
                _content.Append(content);
            return content;
        }

        public JsonObject ToAssistantMessage()
        {
            var message = new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = _content.ToString()
            };
            if (_hasReasoning)
                message["reasoning_content"] = _reasoning.ToString();

            var toolCalls = new JsonArray();
            foreach (var call in ToolCalls)
            {
                toolCalls.Add(new JsonObject
                {
                    ["id"] = call.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.Arguments }
                });
            }
            message["tool_calls"] = toolCalls;
            return message;
        }

        private void ReadUsage(JsonNode? usage)
        {
            if (usage is not JsonObject)
                return;
            InputTokens = usage["prompt_tokens"]?.GetValue<long>() ?? InputTokens;
            OutputTokens = usage["completion_tokens"]?.GetValue<long>() ?? OutputTokens;
        }
    }

    private record SearchHit(string Title, string Url, string Site, string Date, string Content);

    /// <summary>Either Hits (possibly empty) or Error is set.</summary>
    private record ToolOutcome(string ToolCallId, string Name, IReadOnlyList<SearchHit>? Hits, string? Error);

    private static string? GetQuery(string arguments)
    {
        try
        {
            var query = JsonNode.Parse(arguments)?["query"]?.GetValue<string>();
            return string.IsNullOrWhiteSpace(query) ? null : query;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<ToolOutcome> RunToolCallAsync(ToolCall call, CancellationToken ct)
    {
        if (call.Name != WebSearchToolName)
        {
            _logger.LogWarning("Kimi called unknown tool {Tool}", call.Name);
            return new ToolOutcome(call.Id, call.Name, null, $"Error: unknown tool '{call.Name}'.");
        }

        string? query = GetQuery(call.Arguments);
        if (query is null)
            return new ToolOutcome(call.Id, call.Name, null, "Error: 'query' is required.");

        _logger.LogInformation("Kimi web_search: {Query}", query);
        try
        {
            return new ToolOutcome(call.Id, call.Name, await SearchAsync(query, ct), null);
        }
        catch (ClientResultException ex)
        {
            // One failed search shouldn't kill the whole analysis; let the model carry on without it.
            _logger.LogWarning(ex, "Kimi web_search failed. Status: {Status}. Query: {Query}", ex.Status, query);
            return new ToolOutcome(call.Id, call.Name, null, "Error: web_search failed. Continue without this result.");
        }
    }

    private async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct)
    {
        // The slot is held through retries too, so a rate-limited search doesn't let another one in to pile on.
        JsonNode json;
        await _searchSlots.WaitAsync(ct);
        try
        {
            json = await SendAsync("POST", "tools/search_pro", new JsonObject
            {
                ["text_query"] = query,
                ["limit"] = _options.SearchResultsPerQuery
            }, ct);
        }
        finally
        {
            _searchSlots.Release();
        }

        var hits = new List<SearchHit>();
        foreach (var item in json["search_results"] as JsonArray ?? [])
        {
            string url = AsText(item?["url"]) ?? "";
            if (string.IsNullOrWhiteSpace(url))
                continue;

            // Best-scoring chunks first; fall back to the snippet when the page has no chunks.
            var chunks = (item!["chunks"] as JsonArray ?? [])
                .OrderByDescending(c => c?["score"]?.GetValue<double>() ?? 0)
                .Select(c => AsText(c?["text"]))
                .Where(t => !string.IsNullOrWhiteSpace(t));
            string content = string.Join("\n", chunks);
            if (string.IsNullOrWhiteSpace(content))
                content = AsText(item["snippet"]) ?? "";

            if (content.Length > MaxCharsPerResult)
                content = content[..MaxCharsPerResult] + "…";

            hits.Add(new SearchHit(
                Title: AsText(item["title"]) ?? "",
                Url: url,
                Site: AsText(item["site_name"]) ?? "",
                Date: AsText(item["date"]) ?? "",
                Content: content));
        }
        return hits;
    }

    /// <summary>Numbers the hits (a URL seen in an earlier search keeps its number) and renders them for the model.</summary>
    private static string FormatResults(IReadOnlyList<SearchHit> hits, SourceCollector sources)
    {
        if (hits.Count == 0)
            return "No results for this query.";

        var sb = new StringBuilder();
        foreach (var hit in hits)
        {
            int number = sources.Add(hit.Title, hit.Url);
            sb.AppendLine($"[{number}] {hit.Title}");
            sb.AppendLine($"URL: {hit.Url}");
            if (hit.Site != "" || hit.Date != "")
                sb.AppendLine($"Site: {hit.Site} | Date: {hit.Date}");
            sb.AppendLine(hit.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Stable citation numbers per URL for one request. Used sequentially, not thread-safe.</summary>
    private class SourceCollector
    {
        private readonly Dictionary<string, int> _numberByUrl = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AiSource> _sources = [];

        public int Add(string title, string url)
        {
            if (_numberByUrl.TryGetValue(url, out int existing))
                return existing;

            int number = _sources.Count + 1;
            _sources.Add(new AiSource(number, string.IsNullOrWhiteSpace(title) ? url : title, url));
            _numberByUrl[url] = number;
            return number;
        }

        public IReadOnlyList<AiSource> ToList() => _sources.ToArray();
    }

    /// <summary>
    /// Raw request to a non-chat Moonshot endpoint, sent through <see cref="_searchPipeline"/>:
    /// same auth and timeout as the chat calls, but retried only on HTTP 429.
    /// </summary>
    private async Task<JsonNode> SendAsync(string method, string path, JsonNode? body, CancellationToken ct)
    {
        using PipelineMessage message = _searchPipeline.CreateMessage();
        message.Apply(new RequestOptions { CancellationToken = ct });

        var request = message.Request;
        request.Method = method;
        request.Uri = new Uri($"{_baseUrl}/{path}");
        request.Headers.Set("Accept", "application/json");
        if (body is not null)
        {
            request.Headers.Set("Content-Type", "application/json");
            request.Content = BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
        }

        await _searchPipeline.SendAsync(message);

        var response = message.Response!;
        if (response.IsError)
            throw new ClientResultException(response);

        return JsonNode.Parse(response.Content.ToString())
            ?? throw new InvalidOperationException($"Empty response from {method} {path}");
    }


    private class RateLimitRetryPolicy(int maxRetries, ILogger logger) : ClientRetryPolicy(maxRetries)
    {
        protected override bool ShouldRetry(PipelineMessage message, Exception? exception)
            => IsRateLimited(message, exception) && LogRetry(message, base.ShouldRetry(message, exception));

        protected override async ValueTask<bool> ShouldRetryAsync(PipelineMessage message, Exception? exception)
            => IsRateLimited(message, exception) && LogRetry(message, await base.ShouldRetryAsync(message, exception));

        /// <summary>
        /// Base delay (exponential, or Retry-After when the server sends one) plus up to 1s of random jitter,
        /// so searches rejected together don't all retry at the same instant and get rejected again.
        /// </summary>
        protected override TimeSpan GetNextDelay(PipelineMessage message, int tryCount)
            => base.GetNextDelay(message, tryCount) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));

        private static bool IsRateLimited(PipelineMessage message, Exception? exception)
            => exception is null && message.Response?.Status == 429;

        private bool LogRetry(PipelineMessage message, bool retrying)
        {
            if (retrying)
                logger.LogWarning("Moonshot rate-limited {Uri} (HTTP 429), retrying", message.Request.Uri);
            return retrying;
        }
    }

    private static string? AsText(JsonNode? node) => node switch
    {
        null => null,
        JsonValue v when v.TryGetValue(out string? s) => s,
        _ => node.ToJsonString()
    };

}


internal static class KimiSchema
{
    public static JsonObject From<T>()
    {
        var schema = StructuredOutput.ToJsonSchema<T>();
        MakeStrict(schema);
        return schema;
    }

    private static void MakeStrict(JsonNode? node)
    {
        if (node is JsonArray array)
        {
            foreach (var item in array)
                MakeStrict(item);
            return;
        }

        if (node is not JsonObject obj)
            return;

        if (obj["properties"] is JsonObject properties)
        {
            var requird = obj["required"] as JsonArray;
            if (requird is null)
            {
                requird = new JsonArray();
                obj["required"] = requird;
            }

            var requiredNames = requird.Select(r => r?.GetValue<string>()).ToHashSet();
            foreach (var (name, property) in properties)
            {
                if (requiredNames.Contains(name))
                    continue;

                if (property is JsonObject p && p["type"] is JsonValue type && type.TryGetValue(out string? typeName))
                    p["type"] = new JsonArray(typeName, "null");

                requird.Add(name);
            }
        }

        foreach (var (_, child) in obj.ToList())
            MakeStrict(child);
    }
}
