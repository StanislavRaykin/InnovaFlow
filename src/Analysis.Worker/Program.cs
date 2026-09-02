using Analysis.Worker;
using InnovaFlow.Analysis.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddAnalysisData(builder.Configuration.GetConnectionString("innovaflow")!);

var host = builder.Build();
host.Run();
