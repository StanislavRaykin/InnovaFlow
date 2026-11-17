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

// AI
builder.Services.AddAIClient<ClaudeOptions, ClaudeAiClient>(builder.Configuration, "Claude");
builder.Services.AddAIClient<KimiOptions, KimiClient>(builder.Configuration, "Kimi");

builder.Services.AddSingleton<IAIClientFactory, AIClientFactory>();

// Modules
builder.Services.AddScoped<IAnalysisModule, MarketAnalysisModule>();
builder.Services.AddScoped<IAnalysisModule, AudienceAnalysisModule>();
builder.Services.AddScoped<IAnalysisModule, InnovationAnalysisModule>();

// Messaging
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<AnalysisRequestedConsumer>();

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
host.Services.GetRequiredService<ILogger<Program>>().LogInformation("Analysis.Worker starting");
host.Run();