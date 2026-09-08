using InnovaFlow.Notifier.Data;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

string cs = builder.Configuration.GetConnectionString("InnovaFlow")!;
builder.Services.AddDbContext<NotifierDbContext>(o =>
    o.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "Notifier")));



var app = builder.Build();

// migrations
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<NotifierDbContext>()
               .Database.MigrateAsync();
}

app.MapGet("/", () => "Hello World!");

app.Run();
