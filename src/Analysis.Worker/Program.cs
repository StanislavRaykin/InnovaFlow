using Analysis.Worker;
using RabbitMQ;
using InnovaFlow.Analysis.Data;
using Microsoft.EntityFrameworkCore;
using Analysis.Worker.AI;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton(TimeProvider.System);

//AI configuration
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai:Claude"));

//Add HttpClient for Claude AI
builder.Services.AddHttpClient("claude", client =>
{
    var aiOptions = builder.Configuration.GetSection("Ai:Claude").Get<AiOptions>()!;
    client.BaseAddress = new Uri(aiOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiOptions.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-API-Key", aiOptions.ApiKey);
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    // x.AddConsumer<AnalysisRequestedConsumer>(); // uncomment when ur done with start analysis and 

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbit"));

        cfg.ReceiveEndpoint("analysis-requested", e =>
        {
            e.PrefetchCount = 1;
            e.ConcurrentMessageLimit = 1;
            e.UseMessageRetry(r => r.None());

            // e.ConfigureConsumer<AnalysisRequestedConsumer>(ctx);
        });
    });
});
builder.Services.AddAnalysisData(builder.Configuration.GetConnectionString("innovaflow")!);

var host = builder.Build();
host.Run();
