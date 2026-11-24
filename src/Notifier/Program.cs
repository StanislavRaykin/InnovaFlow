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
builder.AddJwtAuthentication(true);

builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, SubUserIdProvider>();
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<AnalysisProgressConsumer>();
    x.AddConsumer<AnalysisPartialConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging") ?? throw new ArgumentNullException("RabbitMQ connection string is missing. Check AppHost"));
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
