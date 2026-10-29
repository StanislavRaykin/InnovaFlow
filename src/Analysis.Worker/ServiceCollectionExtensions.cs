using Analysis.Worker.AI;

namespace Analysis.Worker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIClient<TOptions, TClient>(this IServiceCollection services, IConfiguration config, string sectionName) where TOptions : AiOptions where TClient : class, IAiClient
    {
        services.AddOptions<AiOptions>()
    .Bind(config.GetSection($"Ai:{sectionName}"))
    .Validate(o => o.MaxTokens > 0, "MaxTokens must be > 0")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Model), "Model is required")
    .ValidateOnStart();

        services.AddKeyedSingleton<IAiClient, TClient>(sectionName);
        return services;


    }

}
