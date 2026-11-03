using Anthropic;
using Microsoft.Extensions.Options;
using Anthropic.Core;
using Anthropic.Models.Organization.ApiKeys;
using InnovaFlow.Analysis.Data;
using Anthropic.Models.Messages;
using Anthropic.Helpers;
using System.Text;
using System.Diagnostics;
using Anthropic.Exceptions;
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

    public async Task<AiResult<T>> CompleteAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class, new()
    {
        if (userPrompt == null)
            throw new ArgumentException("User prompt cannot be null", nameof(userPrompt));

        var userMessages = new List<MessageParam>()
        {
            new MessageParam(new UserMessageParam(userPrompt))
        };


        var messageParams = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = systemPrompt,
            Messages = userMessages,
            OutputConfig = new OutputConfig { Format = StructuredOutput.CreateJsonFormat<T>() }
        };

        Stopwatch sw = Stopwatch.StartNew();
        Message message;

        try
        {
            message = await _client.Messages.Create(messageParams, ct);

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

        var text = new StringBuilder();
            foreach (var block in message.Content)
            {
                if (block.TryPickText(out var textBlock))
                    text.Append(textBlock.Text);
            }

        AiResponse response = new AiResponse(
            Provider: "Claude",
            Text: text.ToString(),
            Model: message.Model.Raw(),
            StopReason: message.StopReason?.Raw(),
            InputTokens: message.Usage.InputTokens,
            OutputTokens: message.Usage.OutputTokens,
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

        try
        {
            var result = StructuredOutput.Parse<T>(response.Text);
            return new AiResult<T>(result, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response. Response text: {ResponseText}", response.Text);
            throw new AiClientException("Failed to parse Claude response", ex, response);
        }

    }

}