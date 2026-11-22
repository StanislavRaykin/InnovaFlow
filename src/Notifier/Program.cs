using InnovaFlow.Notifier.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Notifier.Events;
var builder = WebApplication.CreateBuilder(args);

string cs = builder.Configuration.GetConnectionString("InnovaFlow")!;
builder.Services.AddDbContext<NotifierDbContext>(o =>
    o.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "Notifier")));

builder.AddServiceDefaults();
var authBuilder = builder.AddJwtAuthentication();

authBuilder.AddJwtBearer(o =>
    {
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSignalR();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AnalysisProgressConsumer>();
    x.AddConsumer<AnalysisPartialConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq") ?? throw new ArgumentNullException("RabbitMQ connection string is missing. Check AppHost"));

        cfg.ReceiveEndpoint(new TemporaryEndpointDefinition(), e =>
        {
            e.ConcurrentMessageLimit = 2;
            e.ConfigureConsumer<AnalysisProgressConsumer>(ctx);
            e.ConfigureConsumer<AnalysisPartialConsumer>(ctx);
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

// migrations
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<NotifierDbContext>()
               .Database.MigrateAsync();
}

app.MapHub<AnalysisHub>("/hubs/analysis");
app.MapDefaultEndpoints();
app.MapGet("/", () => "Hello World!");

app.Run();
