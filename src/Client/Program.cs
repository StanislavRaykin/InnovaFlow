using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Without this the API calls would hit the Blazor dev server itself, which answers 405.
var gatewayUrl = builder.Configuration["GatewayUrl"]
    ?? throw new InvalidOperationException("GatewayUrl is missing from wwwroot/appsettings.json.");


//auth
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<TokenStorage>();
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddTransient<BearerTokenHandler>();

// every API call goes through the Gateway with the JWT attached
builder.Services.AddHttpClient("Gateway", c => c.BaseAddress = new Uri(gatewayUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gateway"));

//services
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
