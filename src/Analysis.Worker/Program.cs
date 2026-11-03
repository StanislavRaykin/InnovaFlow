using Analysis.Worker;
using Analysis.Worker.AI;
using Analysis.Worker.Consumer;
using Analysis.Worker.Modules;
using Analysis.Worker.Modules.Market;
using InnovaFlow.Analysis.Data;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var dbConnection = builder.Configuration.GetConnectionString("innovaflow")
    ?? throw new InvalidOperationException("Missing 'innovaflow' connection string. Check AppHost .WithReference(db).");
var rabbitConnection = builder.Configuration.GetConnectionString("messaging")
    ?? throw new InvalidOperationException("Missing 'messaging' connection string. Check AppHost .WithReference(rabbit).");

builder.Services.AddAnalysisData(dbConnection);

// AI
builder.Services.AddAIClient<ClaudeOptions, ClaudeAiClient>(builder.Configuration, "Claude");
builder.Services.AddSingleton<IAIClientFactory, AIClientFactory>();

// Modules
builder.Services.AddScoped<IAnalysisModule, MarketAnalysisModule>();

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

var host = builder.Build();
host.Services.GetRequiredService<ILogger<Program>>().LogInformation("Analysis.Worker starting");
host.Run();