using Analysis.Worker;
using Microsoft.Extensions.Options;
using RabbitMQ;
using InnovaFlow.Analysis.Data;
using Microsoft.EntityFrameworkCore;
using Analysis.Worker.AI;
using MassTransit;
using Analysis.Worker.Consumer;
using MassTransit.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddAnalysisData(builder.Configuration.GetConnectionString("innovaflow")!);

builder.Services.AddSingleton(TimeProvider.System);

//AI configuration
builder.Services.AddAIClient<ClaudeOptions, ClaudeAiClient>(builder.Configuration, "Claude");

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<AnalysisRequestedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
         var connectionString = builder.Configuration.GetConnectionString("messaging")! ?? throw new InvalidOperationException("No rabbitMQ connection string. Check AppHost and Program.cs");
        cfg.Host(new Uri(connectionString));

        cfg.ReceiveEndpoint("analysis-requested", e =>
        {
            e.PrefetchCount = 1;
            e.ConcurrentMessageLimit = 1;
            e.UseMessageRetry(r => r.None());

            e.ConfigureConsumer<AnalysisRequestedConsumer>(ctx);
        });
    });
});

var host = builder.Build();
host.Run();
