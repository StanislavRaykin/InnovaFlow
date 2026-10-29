using Anthropic;
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
    }

    public Task<AiResult<T>> CompleteAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct = default) where T : class
    {
        throw new NotImplementedException();
    }

}