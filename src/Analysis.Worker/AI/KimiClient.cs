using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
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

    private const string WebSearchToolJson = """
        [{
          "type": "function",
          "function": {
            "name": "web_search",
            "description": "Search the web. Returns numbered results with title, URL and relevant excerpts. Cite results by their number, e.g. [3]. Numbers stay the same across searches.",
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

    public Task<AiResponse> CompleteTextAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => RunAsync(systemPrompt, userPrompt, responseFormat: null, ct);

    public async Task<AiResult<T>> CompleteAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
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

        var response = await RunAsync(systemPrompt, userPrompt, responseFormat, ct);

        try
        {
            var result = StructuredOutput.Parse<T>(response.Text);
            return new AiResult<T>(result, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Kimi response. Response text: {ResponseText}", response.Text);
            throw new AiClientException("Failed to parse Kimi response", ex, response);
        }
    }

    /// <summary>The chat + web_search loop shared by both modes. <paramref name="responseFormat"/> null = free text.</summary>
    private async Task<AiResponse> RunAsync(string systemPrompt, string userPrompt, JsonObject? responseFormat, CancellationToken ct)
    {
        if (userPrompt == null)
            throw new ArgumentException("User prompt cannot be null", nameof(userPrompt));

        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
            new JsonObject { ["role"] = "user", ["content"] = userPrompt }
        };

        // Totals across every round of the tool loop: all of it is billed.
        long inputTokens = 0, outputTokens = 0;
        string model = _options.Model;
        string? stopReason = null;
        string text = "";
        int searches = 0;
        var sources = new SourceCollector();

        Stopwatch sw = Stopwatch.StartNew();
        AiResponse Snapshot() => new(text, model, stopReason, inputTokens, outputTokens, sources.ToList(), sw.Elapsed);

        try
        {
            for (int round = 0; ; round++)
            {
                // Out of search rounds: keep the tool declared (history contains tool calls) but forbid new calls.
                bool lastRound = round >= _options.MaxToolRounds;

                var body = new JsonObject
                {
                    ["model"] = _options.Model,
                    ["messages"] = messages.DeepClone(),
                    ["max_completion_tokens"] = _options.MaxTokens,
                    ["reasoning_effort"] = _options.ReasoningEffort,
                    ["tools"] = JsonNode.Parse(WebSearchToolJson),
                    ["tool_choice"] = lastRound ? "none" : "auto"
                };
                if (responseFormat is not null)
                    body["response_format"] = responseFormat.DeepClone();

                ClientResult result = await _chat.CompleteChatAsync(
                    BinaryContent.Create(BinaryData.FromString(body.ToJsonString())),
                    new RequestOptions { CancellationToken = ct });

                var completion = JsonNode.Parse(result.GetRawResponse().Content.ToString())
                    ?? throw new AiClientException("Kimi returned an empty response", response: Snapshot());

                inputTokens += completion["usage"]?["prompt_tokens"]?.GetValue<long>() ?? 0;
                outputTokens += completion["usage"]?["completion_tokens"]?.GetValue<long>() ?? 0;
                model = completion["model"]?.GetValue<string>() ?? model;

                var choice = completion["choices"]?[0]
                    ?? throw new AiClientException("Kimi returned no choices", response: Snapshot());
                stopReason = choice["finish_reason"]?.GetValue<string>();
                var message = choice["message"]?.AsObject()
                    ?? throw new AiClientException("Kimi returned no message", response: Snapshot());

                if (stopReason != "tool_calls" || message["tool_calls"] is not JsonArray { Count: > 0 } toolCalls)
                {
                    text = AsText(message["content"]) ?? "";
                    break;
                }

                if (lastRound)
                    throw new AiClientException($"Kimi kept calling tools after {_options.MaxToolRounds} rounds", response: Snapshot());

                // The full assistant message (with reasoning_content and tool_calls) goes back as-is.
                messages.Add(message.DeepClone());

                // Searches run in parallel; numbering happens afterwards, in tool-call order, so it's deterministic.
                var outcomes = await Task.WhenAll(toolCalls.Select(tc => RunToolCallAsync(tc!, ct)));
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

                searches += toolCalls.Count;
            }
        }
        catch (ClientResultException ex)
        {
            var errorBody = ex.GetRawResponse()?.Content?.ToString();
            _logger.LogError(ex, "Kimi API error occurred. Status: {Status}. Body: {Body}", ex.Status, errorBody);
            throw new AiClientException($"Kimi API error occurred (HTTP {ex.Status})", ex, Snapshot());
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

        return response;
    }

    private record SearchHit(string Title, string Url, string Site, string Date, string Content);

    /// <summary>Either Hits (possibly empty) or Error is set.</summary>
    private record ToolOutcome(string ToolCallId, string Name, IReadOnlyList<SearchHit>? Hits, string? Error);

    private async Task<ToolOutcome> RunToolCallAsync(JsonNode toolCall, CancellationToken ct)
    {
        string id = toolCall["id"]!.GetValue<string>();
        string name = toolCall["function"]?["name"]?.GetValue<string>() ?? "";
        string arguments = toolCall["function"]?["arguments"]?.GetValue<string>() ?? "{}";

        if (name != WebSearchToolName)
        {
            _logger.LogWarning("Kimi called unknown tool {Tool}", name);
            return new ToolOutcome(id, name, null, $"Error: unknown tool '{name}'.");
        }

        string? query;
        try
        {
            query = JsonNode.Parse(arguments)?["query"]?.GetValue<string>();
        }
        catch (Exception)
        {
            query = null;
        }

        if (string.IsNullOrWhiteSpace(query))
            return new ToolOutcome(id, name, null, "Error: 'query' is required.");

        _logger.LogInformation("Kimi web_search: {Query}", query);
        try
        {
            return new ToolOutcome(id, name, await SearchAsync(query, ct), null);
        }
        catch (ClientResultException ex)
        {
            // One failed search shouldn't kill the whole analysis; let the model carry on without it.
            _logger.LogWarning(ex, "Kimi web_search failed. Status: {Status}. Query: {Query}", ex.Status, query);
            return new ToolOutcome(id, name, null, "Error: web_search failed. Continue without this result.");
        }
    }

    private async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct)
    {
        var json = await SendAsync("POST", "tools/search_pro", new JsonObject
        {
            ["text_query"] = query,
            ["limit"] = _options.SearchResultsPerQuery
        }, ct);

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

    private static string StripCodeFences(string text)
    {
        text = text.Trim();
        if (!text.StartsWith("```"))
            return text;

        int firstNewLine = text.IndexOf('\n');
        int closing = text.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && closing > firstNewLine
            ? text[(firstNewLine + 1)..closing].Trim()
            : text;
    }
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
