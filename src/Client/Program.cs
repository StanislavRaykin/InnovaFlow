using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Auth;
using Client.Ideas;
using Client.Analyses;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var gatewayUrl = new Uri(builder.Configuration["GatewayUrl"]
    ?? throw new InvalidOperationException("GatewayUrl is missing from wwwroot/appsettings.json."));



//auth
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<TokenStorage>();
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddTransient<BearerTokenHandler>();

//signalR
builder.Services.AddScoped(sp => new HubConnectionBuilder()
  .WithUrl(new Uri(gatewayUrl, "hubs/analysis"),
           o => o.AccessTokenProvider = () => sp.GetRequiredService<JwtAuthenticationStateProvider>().GetTokenAsync())
  .WithAutomaticReconnect()
  .Build());

builder.Services.AddScoped<HubClient>();
  
// used only by JwtAuthenticationStateProvider to check the session; it sets the token itself
builder.Services.AddHttpClient(JwtAuthenticationStateProvider.SessionClientName, c => c.BaseAddress = gatewayUrl);

// every API call goes through the Gateway with the JWT attached
builder.Services.AddHttpClient("Gateway", c => c.BaseAddress = gatewayUrl)
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gateway"));

//services
builder.Services.AddScoped<IdentityClient>();
builder.Services.AddScoped<IdeaDraft>();
builder.Services.AddScoped<DraftAutosave>();
builder.Services.AddScoped<IdeaClient>();
builder.Services.AddScoped<AnalysisClient>();
builder.Services.AddScoped<AnalysisLiveState>();

await builder.Build().RunAsync();
