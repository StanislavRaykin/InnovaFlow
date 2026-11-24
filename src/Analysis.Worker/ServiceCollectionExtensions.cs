using Analysis.Worker.AI;
using Microsoft.Extensions.Options;

namespace Analysis.Worker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIClient<TOptions, TClient>(this IServiceCollection services, IConfiguration config, string sectionName) where TOptions : AiOptions where TClient : class, IAiClient
    {
        services.AddOptions<TOptions>()
    .Bind(config.GetSection($"Ai:{sectionName}"))
    .Validate(o => o.MaxTokens > 0, "MaxTokens must be > 0")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Model), "Model is required")
    .ValidateOnStart();

        services.AddKeyedSingleton<IAiClient, TClient>(sectionName);
        return services;

    }

    /// <summary>Registers a <see cref="FakeAiClient"/> under each name in place of the real client. No API keys needed.</summary>
    public static IServiceCollection AddFakeAIClients(this IServiceCollection services, IConfiguration config, params string[] names)
    {
        services.AddOptions<FakeAiOptions>().Bind(config.GetSection("Ai:Fake"));

        foreach (var name in names)
        {
            services.AddKeyedSingleton<IAiClient>(name, (sp, _) => new FakeAiClient(
                name,
                sp.GetRequiredService<IOptions<FakeAiOptions>>().Value,
                sp.GetRequiredService<ILogger<FakeAiClient>>()));
        }
        return services;
    }



}
