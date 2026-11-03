using InnovaFlow.Projects.Data;
using Microsoft.EntityFrameworkCore;
using Projects.Data;
using ServiceDefaults;
using FluentValidation;
using Projects.Endpoints;
using Projects.Extensions;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);




// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.AddJwtAuthentication();

string cs = builder.Configuration.GetConnectionString("innovaflow")!;

builder.Services.AddScoped<IActor,HttpActor>();
builder.Services.AddProjectsData(cs);
builder.Services.AddValidatorsFromAssemblyContaining<CreateIdeaRequestValidator>();

var app = builder.Build();

// migrations
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ProjectsDbContext>().Database.MigrateAsync();
    
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapEndpoints();
app.MapDefaultEndpoints();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public sealed class HttpActor(ICurrentUser me) : IActor
{
    public Guid? Id => me.IdOrNull;
    public bool IsSystem => false;
}