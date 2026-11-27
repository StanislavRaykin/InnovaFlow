using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
namespace Analysis.Worker.AI;

public sealed class ClaudeAiClient : IAiClient
{
    private readonly AnthropicClient _client;
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeAiClient> _logger;

    public ClaudeAiClient(IOptions<ClaudeOptions> options, ILogger<ClaudeAiClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            throw new ArgumentNullException("Claude API key not found. Check AppHost");
        }

        _client = new AnthropicClient { ApiKey = _options.ApiKey, MaxRetries = _options.MaxRetries, Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds) };
    }

    public IAsyncEnumerable<AiStreamEvent> StreamTextAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => StreamCoreAsync(systemPrompt, userPrompt, outputConfig: null, ct);

    public Task<AiResponse> CompleteTextAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => AiStream.CollectAsync(StreamTextAsync(systemPrompt, userPrompt, ct), ct);

    // With a JSON-schema output format the text deltas are the JSON itself, streamed as it is generated.
    public IAsyncEnumerable<AiStreamEvent> StreamAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
        => AiStream.ParseCompleted<T>(
            StreamCoreAsync(systemPrompt, userPrompt, new OutputConfig { Format = StructuredOutput.CreateJsonFormat<T>() }, ct),
            "Claude", _logger, ct);

    public Task<AiResult<T>> CompleteAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
        => AiStream.CollectAsync<T>(StreamAsync<T>(systemPrompt, userPrompt, ct), ct);

    /// <summary>One streamed Messages call. <paramref name="outputConfig"/> null = free text.</summary>
    private async IAsyncEnumerable<AiStreamEvent> StreamCoreAsync(string systemPrompt, string userPrompt, OutputConfig? outputConfig,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _logger.LogInformation("Claude started scoring");
        if (userPrompt == null)
            throw new ArgumentException("User prompt cannot be null", nameof(userPrompt));

        var messageParams = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = systemPrompt,
            Messages = [new MessageParam(new UserMessageParam(userPrompt))],
            OutputConfig = outputConfig
        };

        Stopwatch sw = Stopwatch.StartNew();
        // Builds the final Message from the same events we forward, so text/usage/stop reason come from the SDK.
        var aggregator = new MessageContentAggregator();

        await using var events = _client.Messages.CreateStreaming(messageParams, ct)
            .CollectAsync(aggregator)
            .GetAsyncEnumerator(ct);
        while (true)
        {
            RawMessageStreamEvent streamEvent;
            try
            {
                if (!await events.MoveNextAsync())
                    break;
                streamEvent = events.Current;
            }
            catch (AnthropicApiException ex)
            {
                _logger.LogError(ex, "Anthropic API error occurred");
                throw new AnthropicServiceException("Anthropic API error occurred", ex);
            }
            catch (AnthropicException ex)
            {
                _logger.LogError(ex, "Anthropic error occurred");
                throw new AnthropicServiceException("Anthropic error occurred", ex);
            }

            if (streamEvent.TryPickContentBlockDelta(out var blockDelta) &&
                blockDelta.Delta.TryPickText(out var textDelta) &&
                textDelta.Text.Length > 0)
            {
                yield return new AiTextDelta(textDelta.Text);
            }
        }

        Message message = aggregator.Message();

        var text = new StringBuilder();
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var textBlock))
                text.Append(textBlock.Text);
        }

        AiResponse response = new AiResponse(
            Text: text.ToString(),
            Model: message.Model.Raw(),
            StopReason: message.StopReason?.Raw(),
            InputTokens: message.Usage.InputTokens,
            OutputTokens: message.Usage.OutputTokens,
            Sources: null, 
            Duration: sw.Elapsed
        );

        if (response.WasRefused)
        {
            _logger.LogWarning("Claude refused to answer the prompt. StopReason: {StopReason}", response.StopReason);
            throw new AiClientException($"Claude refused to answer the prompt. StopReason: {response.StopReason}", response: response);
        }

        if (response.WasTruncated)
        {
            _logger.LogWarning("Claude hit max tokens usage. StopReason: {StopReason}", response.StopReason);
            throw new AiClientException($"Claude hit max tokens usage. StopReason: {response.StopReason}", response: response);
        }

        _logger.LogInformation("Claude finished scoring {Scoring}", response.Text);

        yield return new AiStreamCompleted(response);
    }
}
