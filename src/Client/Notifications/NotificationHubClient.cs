using Client.Auth;
using Contracts.Notifications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace Client.Notifications;


public sealed class NotificationHubClient : IAsyncDisposable
{
    private readonly HubConnection hub;
    private readonly JwtAuthenticationStateProvider auth;
    private Task? start;

    public event Action<PushNotification>? Received;

    public NotificationHubClient(Uri gatewayUrl, JwtAuthenticationStateProvider auth)
    {
        this.auth = auth;
        hub = new HubConnectionBuilder()
            .WithUrl(new Uri(gatewayUrl, "hubs/notifications"), o => o.AccessTokenProvider = auth.GetTokenAsync)
            .WithAutomaticReconnect()
            .Build();

        hub.On<PushNotification>("Notification", n => Received?.Invoke(n));

        auth.AuthenticationStateChanged += OnAuthChanged;
    }

    public Task EnsureStartedAsync()
    {
        if (hub.State == HubConnectionState.Connected) return Task.CompletedTask;

        if (hub.State == HubConnectionState.Disconnected && start is not { IsCompleted: false })
            start = hub.StartAsync();
        return start ?? Task.CompletedTask;
    }

    private void OnAuthChanged(Task<AuthenticationState> state) => _ = hub.StopAsync();

    public async ValueTask DisposeAsync()
    {
        auth.AuthenticationStateChanged -= OnAuthChanged;
        await hub.DisposeAsync();
    }
}
