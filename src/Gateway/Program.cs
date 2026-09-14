var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDiscovery();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();


var app = builder.Build();



app.MapGet("/", () => "Hello World!");

app.Run();
