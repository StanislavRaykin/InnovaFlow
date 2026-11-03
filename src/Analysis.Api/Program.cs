using Analysis.Api.Clients;
using Analysis.Api.Endpoints;
using Humanizer;
using InnovaFlow.Analysis.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddJwtAuthentication();

builder.Services.AddAnalysisData(builder.Configuration.GetConnectionString("innovaflow")!);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardTokenHandler>();
// AddServiceDefaults already adds service discovery and the resilience handler to every HttpClient
builder.Services.AddHttpClient<IdeasClient>(c => c.BaseAddress = new Uri("https+http://projects"))
.AddHttpMessageHandler<ForwardTokenHandler>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

   
    x.AddEntityFrameworkOutbox<AnalysisDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging")! ?? throw new InvalidOperationException("No rabbitMQ connection string. Check AppHost and Program.cs");
        cfg.Host(new Uri(connectionString));
        cfg.ConfigureEndpoints(ctx);
    });
});
var app = builder.Build();

// migrations
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AnalysisDbContext>()
               .Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapAnalysisEndpoints();


app.Run();

