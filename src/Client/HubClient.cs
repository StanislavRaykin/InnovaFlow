using Microsoft.AspNetCore.SignalR.Client;
namespace Client;

public class HubClient(HubConnection hub)
{
    private Task? _start;
    public HubConnection Connection => hub;

    public Task EnsureStartedAsync()
    {
        if (hub.State == HubConnectionState.Connected) return Task.CompletedTask;
        
        if (hub.State == HubConnectionState.Disconnected && _start is not { IsCompleted: false })
            _start = hub.StartAsync();
        return _start ?? Task.CompletedTask; 
    }
}