using Analysis.Worker;
using Analysis.Worker.AI;
using Analysis.Worker.Consumer;
using Analysis.Worker.Modules;
using Analysis.Worker.Modules.Innovation;
using Analysis.Worker.Modules.Market;
using InnovaFlow.Analysis.Data;
using MassTransit;
using Analysis.Worker.Modules.Audience;
using Analysis.Worker.Publishers;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var dbConnection = builder.Configuration.GetConnectionString("innovaflow")
    ?? throw new ArgumentNullException("Missing 'innovaflow' connection string. Check AppHost .WithReference(db).");
var rabbitConnection = builder.Configuration.GetConnectionString("messaging")
    ?? throw new ArgumentNullException("Missing 'messaging' connection string. Check AppHost .WithReference(rabbit).");

builder.Services.AddAnalysisData(dbConnection);

// AI. Ai:UseFake = true swaps in FakeAiClient under the same names: dummy results, no API calls, no balance used
var useFakeAi = builder.Configuration.GetValue<bool>("Ai:UseFake");
if (useFakeAi)
{
    builder.Services.AddFakeAIClients(builder.Configuration, "Claude", "Kimi");
}
else
{
    builder.Services.AddAIClient<ClaudeOptions, ClaudeAiClient>(builder.Configuration, "Claude");
    builder.Services.AddAIClient<KimiOptions, KimiClient>(builder.Configuration, "Kimi");
}

builder.Services.AddSingleton<IAIClientFactory, AIClientFactory>();

// Modules
builder.Services.AddScoped<ModuleRunTracker>();

builder.Services.AddScoped<IAnalysisModule, MarketAnalysisModule>();
builder.Services.AddScoped<IAnalysisModule, AudienceAnalysisModule>();
builder.Services.AddScoped<IAnalysisModule, InnovationAnalysisModule>();

// Messaging
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    // Analyses are long-running, so each instance only takes what it can run right now.
    // With the default prefetch (CPU count * 2) one instance would grab a batch of jobs while other replicas sit idle.
    var concurrentAnalyses = builder.Configuration.GetValue("Worker:ConcurrentAnalyses", 4);
    x.AddConsumer<AnalysisRequestedConsumer>()
     .Endpoint(e =>
     {
         e.ConcurrentMessageLimit = concurrentAnalyses;
         e.PrefetchCount = concurrentAnalyses;
     });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitConnection));
        cfg.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60)));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<PartialPublisher>();
builder.Services.AddScoped<ProgressPublisher>();

var host = builder.Build();
var startupLogger = host.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Analysis.Worker starting");
if (useFakeAi)
    startupLogger.LogWarning("Ai:UseFake is on: AI clients return DUMMY data and make no API calls");
host.Run();