using InnovaFlow.Analysis.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAnalysisData(builder.Configuration.GetConnectionString("innovaflow")!);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddServiceDefaults();
builder.AddJwtAuthentication();


builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // Transactional outbox: the AnalysisRequested message is written to Postgres in the
    // same transaction as the Analyses row, then delivered after commit. This is the answer
    // to "how do you guarantee the row and the message agree?"
    x.AddEntityFrameworkOutbox<AnalysisDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbit"));
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



app.Run();

